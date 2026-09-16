using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Inmobiliaria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Inmobiliaria.Controllers;

public class UsuariosController : Controller
{
    private readonly string _connectionString;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public UsuariosController(IConfiguration configuration) =>
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

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
    public IActionResult Perfil() => View();

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
