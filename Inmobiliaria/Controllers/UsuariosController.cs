using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Inmobiliaria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Cryptography;

namespace Inmobiliaria.Controllers;

public class UsuariosController : Controller
{
    private readonly string _connectionString;
    private readonly PasswordHasher<Usuario> _hasher = new();
    private readonly IWebHostEnvironment _environment;

    public UsuariosController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _environment = environment;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        Usuario? usuario = null;
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand("SELECT Id, Email, PasswordHash, Rol, Activo FROM Usuarios WHERE Email = @email LIMIT 1", connection);
        command.Parameters.AddWithValue("@email", model.Email);
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
                usuario = new Usuario { Id = reader.GetInt32("Id"), Email = reader.GetString("Email"), PasswordHash = reader.GetString("PasswordHash"), Rol = reader.GetString("Rol"), Activo = reader.GetBoolean("Activo") };
        }
        if (usuario is null || !usuario.Activo || _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View(model);
        }
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()), new Claim(ClaimTypes.Name, usuario.Email), new Claim(ClaimTypes.Role, usuario.Rol) };
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
        using var update = new MySqlCommand("UPDATE Usuarios SET UltimoAcceso = UTC_TIMESTAMP() WHERE Id = @id", connection);
        update.Parameters.AddWithValue("@id", usuario.Id);
        update.ExecuteNonQuery();
        return LocalRedirect(model.ReturnUrl is { Length: > 0 } && Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : "/");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Index()
    {
        var usuarios = new List<Usuario>();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand("SELECT Id, Email, Rol, Activo, FechaCreacion, UltimoAcceso FROM Usuarios ORDER BY Email", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read()) usuarios.Add(new Usuario { Id = reader.GetInt32("Id"), Email = reader.GetString("Email"), Rol = reader.GetString("Rol"), Activo = reader.GetBoolean("Activo"), FechaCreacion = reader.GetDateTime("FechaCreacion"), UltimoAcceso = reader.IsDBNull(reader.GetOrdinal("UltimoAcceso")) ? null : reader.GetDateTime("UltimoAcceso") });
        return View(usuarios);
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Crear() => View(new CrearUsuarioViewModel());

    [HttpPost, Authorize(Roles = "Administrador"), ValidateAntiForgeryToken]
    public IActionResult Crear(CrearUsuarioViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand(
            "INSERT INTO Usuarios (Email, PasswordHash, Rol) VALUES (@email, @passwordHash, @rol)", connection);
        command.Parameters.AddWithValue("@email", model.Email);
        command.Parameters.AddWithValue("@passwordHash", _hasher.HashPassword(new Usuario(), model.Password));
        command.Parameters.AddWithValue("@rol", model.Rol);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese email.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public IActionResult Perfil()
    {
        var usuarioId = UsuarioActualId();
        if (usuarioId == null) return Unauthorized();
        var usuario = ObtenerUsuarioPerfil(usuarioId.Value);
        return usuario == null
            ? NotFound()
            : View(new PerfilUsuarioViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                AvatarActual = usuario.AvatarUrl
            });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Perfil(PerfilUsuarioViewModel model)
    {
        var usuarioId = UsuarioActualId();
        if (usuarioId == null || usuarioId.Value != model.Id) return Unauthorized();

        var usuario = ObtenerUsuarioPerfil(usuarioId.Value);
        if (usuario == null) return NotFound();

        model.AvatarActual = usuario.AvatarUrl;
        var cambiarPassword = !string.IsNullOrWhiteSpace(model.NuevaPassword);
        if (cambiarPassword)
        {
            if (string.IsNullOrWhiteSpace(model.PasswordActual)
                || _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.PasswordActual) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(model.PasswordActual), "La contraseña actual no es válida.");
            }
            if (model.NuevaPassword != model.ConfirmarPassword)
                ModelState.AddModelError(nameof(model.ConfirmarPassword), "La confirmación no coincide con la nueva contraseña.");
        }
        else if (!string.IsNullOrEmpty(model.ConfirmarPassword))
        {
            ModelState.AddModelError(nameof(model.NuevaPassword), "Ingrese una nueva contraseña para confirmar el cambio.");
        }

        if (model.Avatar is not null && !await EsImagenPermitidaAsync(model.Avatar))
            ModelState.AddModelError(nameof(model.Avatar), "Seleccione una imagen PNG, JPEG o WebP de hasta 2 MB.");

        if (!ModelState.IsValid) return View(model);

        var nuevoAvatarUrl = usuario.AvatarUrl;
        string? nuevoAvatarPath = null;
        if (model.Avatar is not null)
        {
            var extension = Path.GetExtension(model.Avatar.FileName).ToLowerInvariant();
            var nombreArchivo = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}{extension}";
            var directorio = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(directorio);
            nuevoAvatarPath = Path.Combine(directorio, nombreArchivo);
            await using (var fileStream = new FileStream(nuevoAvatarPath, FileMode.CreateNew, FileAccess.Write))
                await model.Avatar.CopyToAsync(fileStream);
            nuevoAvatarUrl = $"/uploads/avatars/{nombreArchivo}";
        }

        var passwordHash = cambiarPassword
            ? _hasher.HashPassword(usuario, model.NuevaPassword!)
            : usuario.PasswordHash;
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var command = new MySqlCommand(
                @"UPDATE Usuarios
                  SET Email = @email, Nombre = @nombre, Apellido = @apellido,
                      AvatarUrl = @avatarUrl, PasswordHash = @passwordHash
                  WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@email", model.Email);
            command.Parameters.AddWithValue("@nombre", model.Nombre);
            command.Parameters.AddWithValue("@apellido", model.Apellido);
            command.Parameters.AddWithValue("@avatarUrl", (object?)nuevoAvatarUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            command.Parameters.AddWithValue("@id", usuarioId.Value);
            if (command.ExecuteNonQuery() != 1)
            {
                if (nuevoAvatarPath is not null) System.IO.File.Delete(nuevoAvatarPath);
                return NotFound();
            }
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            if (nuevoAvatarPath is not null) System.IO.File.Delete(nuevoAvatarPath);
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese email.");
            return View(model);
        }
        catch
        {
            if (nuevoAvatarPath is not null && System.IO.File.Exists(nuevoAvatarPath))
                System.IO.File.Delete(nuevoAvatarPath);
            throw;
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.Value.ToString()),
            new Claim(ClaimTypes.Name, model.Email),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
        TempData["Mensaje"] = "El perfil se actualizó correctamente.";
        return RedirectToAction(nameof(Perfil));
    }

    private Usuario? ObtenerUsuarioPerfil(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand(
            "SELECT Id, Email, PasswordHash, Rol, Nombre, Apellido, AvatarUrl FROM Usuarios WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Usuario
            {
                Id = reader.GetInt32("Id"),
                Email = reader.GetString("Email"),
                PasswordHash = reader.GetString("PasswordHash"),
                Rol = reader.GetString("Rol"),
                Nombre = reader.GetString("Nombre"),
                Apellido = reader.GetString("Apellido"),
                AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? null : reader.GetString("AvatarUrl")
            }
            : null;
    }

    private int? UsuarioActualId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static async Task<bool> EsImagenPermitidaAsync(IFormFile archivo)
    {
        if (archivo.Length is <= 0 or > 2 * 1024 * 1024) return false;
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var encabezado = new byte[12];
        await using var stream = archivo.OpenReadStream();
        var bytesLeidos = await stream.ReadAsync(encabezado.AsMemory());
        return extension switch
        {
            ".png" => bytesLeidos >= 8 && encabezado.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => bytesLeidos >= 3 && encabezado[0] == 255 && encabezado[1] == 216 && encabezado[2] == 255,
            ".webp" => bytesLeidos >= 12 && encabezado.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                       && encabezado.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => Forbid();
}

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class CrearUsuarioViewModel
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, RegularExpression("Administrador|Empleado")]
    public string Rol { get; set; } = "Empleado";
}
