using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;

namespace Inmobiliaria.Controllers;

[Authorize]
public class InmueblesController : Controller
{
    private const long TamanoMaximoImagen = 5 * 1024 * 1024;
    private const int MaximoImagenesAdicionalesPorCarga = 10;
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<InmueblesController> _logger;

    public InmueblesController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<InmueblesController> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        _environment = environment;
        _logger = logger;
    }

    private void CargarSelectLabels(Inmueble inmueble)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        ViewBag.PropietarioTexto = BuscarEtiqueta(connection,
            "SELECT CONCAT(Apellido, ', ', Nombre, ' · DNI ', Dni) FROM Propietarios WHERE Id = @id", inmueble.PropietarioId);
        ViewBag.TipoTexto = BuscarEtiqueta(connection,
            "SELECT Descripcion FROM TiposInmueble WHERE Id = @id", inmueble.TipoInmuebleId);
    }

    private static string? BuscarEtiqueta(MySqlConnection connection, string sql, int id)
    {
        if (id <= 0) return null;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        return command.ExecuteScalar()?.ToString();
    }

    private bool ValidarReferencias(MySqlConnection connection, Inmueble inmueble)
    {
        using var propietarioCommand = new MySqlCommand(
            "SELECT COUNT(*) FROM Propietarios WHERE Id = @id", connection);
        propietarioCommand.Parameters.AddWithValue("@id", inmueble.PropietarioId);
        if (inmueble.PropietarioId <= 0 || Convert.ToInt32(propietarioCommand.ExecuteScalar()) == 0)
            ModelState.AddModelError(nameof(inmueble.PropietarioId), "Seleccione un propietario válido.");

        using var tipoCommand = new MySqlCommand(
            "SELECT COUNT(*) FROM TiposInmueble WHERE Id = @id", connection);
        tipoCommand.Parameters.AddWithValue("@id", inmueble.TipoInmuebleId);
        if (inmueble.TipoInmuebleId <= 0 || Convert.ToInt32(tipoCommand.ExecuteScalar()) == 0)
            ModelState.AddModelError(nameof(inmueble.TipoInmuebleId), "Seleccione un tipo de inmueble válido.");

        return ModelState.IsValid;
    }

    public IActionResult Index(string? buscar, string? estado, int pagina = 1)
    {
        const int porPagina = 10;
        pagina = Math.Max(1, pagina);
        var inmuebles = new List<Inmueble>();
        var total = 0;
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            const string filtro = @"WHERE (@buscar = '' OR i.Nombre LIKE CONCAT('%', @buscar, '%')
                                            OR i.Direccion LIKE CONCAT('%', @buscar, '%')
                                            OR p.Nombre LIKE CONCAT('%', @buscar, '%')
                                            OR p.Apellido LIKE CONCAT('%', @buscar, '%'))
                                     AND (@estado = '' OR (@estado = 'disponible' AND i.Disponible = 1)
                                          OR (@estado = 'suspendido' AND i.Disponible = 0))";
            var from = @"FROM Inmuebles i
                         INNER JOIN Propietarios p ON i.PropietarioId = p.Id
                         INNER JOIN TiposInmueble t ON i.TipoInmuebleId = t.Id";
            using var totalCommand = new MySqlCommand($"SELECT COUNT(*) {from} {filtro}", connection);
            totalCommand.Parameters.AddWithValue("@buscar", buscar?.Trim() ?? "");
            totalCommand.Parameters.AddWithValue("@estado", estado ?? "");
            total = Convert.ToInt32(totalCommand.ExecuteScalar());
            pagina = Math.Min(pagina, Math.Max(1, (int)Math.Ceiling(total / (double)porPagina)));
            string sql = $@"SELECT i.*, p.Nombre AS PropNombre, p.Apellido AS PropApellido, t.Descripcion AS TipoDesc
                            {from} {filtro}
                            ORDER BY i.Nombre, i.Id LIMIT @limite OFFSET @offset";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@buscar", buscar?.Trim() ?? "");
            command.Parameters.AddWithValue("@estado", estado ?? "");
            command.Parameters.AddWithValue("@limite", porPagina);
            command.Parameters.AddWithValue("@offset", (pagina - 1) * porPagina);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                inmuebles.Add(MapearInmueble(reader));
            }
        }
        return View(new PaginadoViewModel<Inmueble>
        {
            Elementos = inmuebles,
            Pagina = pagina,
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)porPagina)),
            ValoresRuta = new Dictionary<string, string?> { ["buscar"] = buscar, ["estado"] = estado }
        });
    }

    public IActionResult Details(int id)
    {
        Inmueble? inmueble = null;
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT i.*, p.Nombre AS PropNombre, p.Apellido AS PropApellido, t.Descripcion AS TipoDesc
                           FROM Inmuebles i
                           INNER JOIN Propietarios p ON i.PropietarioId = p.Id
                           INNER JOIN TiposInmueble t ON i.TipoInmuebleId = t.Id
                           WHERE i.Id = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read()) inmueble = MapearInmueble(reader);
        }

        if (inmueble == null) return NotFound();
        return View(inmueble);
    }

    public IActionResult Create()
    {
        var inmueble = new Inmueble();
        CargarSelectLabels(inmueble);
        return View(inmueble);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Create(Inmueble inmueble)
    {
        if (!await ValidarImagenesAsync(inmueble, portadaRequerida: true))
        {
            CargarSelectLabels(inmueble);
            return View(inmueble);
        }
        if (!ModelState.IsValid)
        {
            CargarSelectLabels(inmueble);
            return View(inmueble);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            if (!ValidarReferencias(connection, inmueble))
            {
                CargarSelectLabels(inmueble);
                return View(inmueble);
            }
            var imagenesGuardadas = await GuardarImagenesAsync(
                new[] { inmueble.ImagenPortadaArchivo! }.Concat(inmueble.ImagenesAdicionalesArchivos));
            inmueble.ImagenPortada = imagenesGuardadas[0];
            inmueble.ImagenesAdicionales = imagenesGuardadas.Count > 1
                ? string.Join(',', imagenesGuardadas.Skip(1))
                : null;
            try
            {
                string sql = @"INSERT INTO Inmuebles
                    (Nombre, Descripcion, Direccion, Cupo, Latitud, Longitud, PrecioPorDia, PorcentajeReserva, ImagenPortada, ImagenesAdicionales, Disponible, PropietarioId, TipoInmuebleId)
                    VALUES (@nombre, @desc, @dir, @cupo, @lat, @lng, @precio, @porc, @imagenPortada, @imagenesAdicionales, @disp, @propId, @tipoId)";
                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@nombre", inmueble.Nombre);
                cmd.Parameters.AddWithValue("@desc", inmueble.Descripcion);
                cmd.Parameters.AddWithValue("@dir", inmueble.Direccion);
                cmd.Parameters.AddWithValue("@cupo", inmueble.Cupo);
                cmd.Parameters.AddWithValue("@lat", (object?)inmueble.Latitud ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@lng", (object?)inmueble.Longitud ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@precio", inmueble.PrecioPorDia);
                cmd.Parameters.AddWithValue("@porc", inmueble.PorcentajeReserva);
                cmd.Parameters.AddWithValue("@imagenPortada", inmueble.ImagenPortada);
                cmd.Parameters.AddWithValue("@imagenesAdicionales", (object?)inmueble.ImagenesAdicionales ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@disp", inmueble.Disponible ? 1 : 0);
                cmd.Parameters.AddWithValue("@propId", inmueble.PropietarioId);
                cmd.Parameters.AddWithValue("@tipoId", inmueble.TipoInmuebleId);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                EliminarImagenesSubidas(imagenesGuardadas);
                throw;
            }
        }
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var inmueble = ObtenerPorId(id);
        if (inmueble == null) return NotFound();
        CargarSelectLabels(inmueble);
        return View(inmueble);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, Inmueble inmueble)
    {
        if (id != inmueble.Id) return BadRequest();
        var inmuebleActual = ObtenerPorId(id);
        if (inmuebleActual == null) return NotFound();

        inmueble.ImagenPortada = inmuebleActual.ImagenPortada;
        inmueble.ImagenesAdicionales = inmuebleActual.ImagenesAdicionales;
        if (!await ValidarImagenesAsync(inmueble, portadaRequerida: false) || !ModelState.IsValid)
        {
            CargarSelectLabels(inmueble);
            return View(inmueble);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            if (!ValidarReferencias(connection, inmueble))
            {
                CargarSelectLabels(inmueble);
                return View(inmueble);
            }
            var imagenesGuardadas = await GuardarImagenesAsync(
                (inmueble.ImagenPortadaArchivo is null
                    ? Enumerable.Empty<IFormFile>()
                    : [inmueble.ImagenPortadaArchivo])
                .Concat(inmueble.ImagenesAdicionalesArchivos));
            var portadaAnterior = inmuebleActual.ImagenPortada;
            var imagenesAdicionales = SepararImagenes(inmuebleActual.ImagenesAdicionales)
                .Concat(imagenesGuardadas.Where((_, index) => inmueble.ImagenPortadaArchivo is null || index > 0))
                .ToList();
            if (inmueble.ImagenPortadaArchivo is not null)
                inmueble.ImagenPortada = imagenesGuardadas[0];
            inmueble.ImagenesAdicionales = imagenesAdicionales.Count == 0
                ? null
                : string.Join(',', imagenesAdicionales);
            try
            {
                string sql = @"UPDATE Inmuebles SET
                    Nombre = @nombre, Descripcion = @desc, Direccion = @dir, Cupo = @cupo,
                    Latitud = @lat, Longitud = @lng, PrecioPorDia = @precio, PorcentajeReserva = @porc,
                    ImagenPortada = @imagenPortada, ImagenesAdicionales = @imagenesAdicionales,
                    Disponible = @disp, PropietarioId = @propId, TipoInmuebleId = @tipoId
                    WHERE Id = @id";
                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@nombre", inmueble.Nombre);
                cmd.Parameters.AddWithValue("@desc", inmueble.Descripcion);
                cmd.Parameters.AddWithValue("@dir", inmueble.Direccion);
                cmd.Parameters.AddWithValue("@cupo", inmueble.Cupo);
                cmd.Parameters.AddWithValue("@lat", (object?)inmueble.Latitud ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@lng", (object?)inmueble.Longitud ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@precio", inmueble.PrecioPorDia);
                cmd.Parameters.AddWithValue("@porc", inmueble.PorcentajeReserva);
                cmd.Parameters.AddWithValue("@imagenPortada", inmueble.ImagenPortada);
                cmd.Parameters.AddWithValue("@imagenesAdicionales", (object?)inmueble.ImagenesAdicionales ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@disp", inmueble.Disponible ? 1 : 0);
                cmd.Parameters.AddWithValue("@propId", inmueble.PropietarioId);
                cmd.Parameters.AddWithValue("@tipoId", inmueble.TipoInmuebleId);
                if (cmd.ExecuteNonQuery() != 1)
                {
                    EliminarImagenesSubidas(imagenesGuardadas);
                    return NotFound();
                }
            }
            catch
            {
                EliminarImagenesSubidas(imagenesGuardadas);
                throw;
            }
            if (inmueble.ImagenPortadaArchivo is not null)
                EliminarImagenSubida(portadaAnterior);
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Delete(int id)
    {
        var inmueble = ObtenerPorId(id);
        return inmueble == null ? NotFound() : View(inmueble);
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var inmueble = ObtenerPorId(id);
        if (inmueble == null) return NotFound();
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "DELETE FROM Inmuebles WHERE Id = @id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", id);
            try
            {
                if (cmd.ExecuteNonQuery() == 0) return NotFound();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                var inmuebleConReservas = ObtenerPorId(id);
                if (inmuebleConReservas == null) return NotFound();
                ModelState.AddModelError(string.Empty, "No se puede eliminar este inmueble porque tiene reservas asociadas.");
                return View("Delete", inmuebleConReservas);
            }
        }
        EliminarImagenSubida(inmueble.ImagenPortada);
        EliminarImagenesSubidas(SepararImagenes(inmueble.ImagenesAdicionales));
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ValidarImagenesAsync(Inmueble inmueble, bool portadaRequerida)
    {
        var imagenesAdicionales = inmueble.ImagenesAdicionalesArchivos
            .Where(imagen => imagen.Length > 0)
            .ToList();
        if (portadaRequerida && (inmueble.ImagenPortadaArchivo is null || inmueble.ImagenPortadaArchivo.Length == 0))
            ModelState.AddModelError(nameof(inmueble.ImagenPortadaArchivo), "La imagen de portada es obligatoria.");
        if (imagenesAdicionales.Count > MaximoImagenesAdicionalesPorCarga)
            ModelState.AddModelError(nameof(inmueble.ImagenesAdicionalesArchivos), $"Puede cargar hasta {MaximoImagenesAdicionalesPorCarga} imágenes adicionales por vez.");

        if (inmueble.ImagenPortadaArchivo is { Length: > 0 }
            && !await EsImagenPermitidaAsync(inmueble.ImagenPortadaArchivo))
            ModelState.AddModelError(nameof(inmueble.ImagenPortadaArchivo), "La portada debe ser PNG, JPEG o WebP y no superar los 5 MB.");

        for (var index = 0; index < imagenesAdicionales.Count; index++)
        {
            if (!await EsImagenPermitidaAsync(imagenesAdicionales[index]))
                ModelState.AddModelError(nameof(inmueble.ImagenesAdicionalesArchivos), $"La imagen adicional {index + 1} debe ser PNG, JPEG o WebP y no superar los 5 MB.");
        }
        return ModelState.IsValid;
    }

    private async Task<List<string>> GuardarImagenesAsync(IEnumerable<IFormFile> archivos)
    {
        var guardadas = new List<string>();
        try
        {
            var directorio = DirectorioImagenes();
            Directory.CreateDirectory(directorio);
            foreach (var archivo in archivos.Where(archivo => archivo.Length > 0))
            {
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                var nombreArchivo = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}{extension}";
                var ruta = Path.Combine(directorio, nombreArchivo);
                await using (var stream = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write))
                    await archivo.CopyToAsync(stream);
                guardadas.Add($"/uploads/inmuebles/{nombreArchivo}");
            }
            return guardadas;
        }
        catch
        {
            EliminarImagenesSubidas(guardadas);
            throw;
        }
    }

    private static async Task<bool> EsImagenPermitidaAsync(IFormFile archivo)
    {
        if (archivo.Length <= 0 || archivo.Length > TamanoMaximoImagen) return false;
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

    private IEnumerable<string> SepararImagenes(string? imagenes) =>
        string.IsNullOrWhiteSpace(imagenes)
            ? []
            : imagenes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private string DirectorioImagenes() =>
        Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "inmuebles");

    private void EliminarImagenesSubidas(IEnumerable<string> imagenes)
    {
        foreach (var imagen in imagenes) EliminarImagenSubida(imagen);
    }

    private void EliminarImagenSubida(string? imagen)
    {
        const string prefijo = "/uploads/inmuebles/";
        if (string.IsNullOrWhiteSpace(imagen) || !imagen.StartsWith(prefijo, StringComparison.Ordinal)) return;
        var ruta = Path.GetFullPath(Path.Combine(DirectorioImagenes(), imagen[prefijo.Length..]));
        var directorio = Path.GetFullPath(DirectorioImagenes()) + Path.DirectorySeparatorChar;
        if (!ruta.StartsWith(directorio, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            if (System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar el archivo de imagen {Imagen}", imagen);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar el archivo de imagen {Imagen}", imagen);
        }
    }

    private Inmueble? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        string sql = "SELECT * FROM Inmuebles WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapearInmuebleSimple(r) : null;
    }

    private Inmueble MapearInmueble(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        Nombre = r["Nombre"].ToString() ?? "",
        Descripcion = r["Descripcion"].ToString() ?? "",
        Direccion = r["Direccion"].ToString() ?? "",
        Cupo = Convert.ToInt32(r["Cupo"]),
        Latitud = r["Latitud"] != DBNull.Value ? Convert.ToDecimal(r["Latitud"]) : null,
        Longitud = r["Longitud"] != DBNull.Value ? Convert.ToDecimal(r["Longitud"]) : null,
        PrecioPorDia = Convert.ToDecimal(r["PrecioPorDia"]),
        PorcentajeReserva = Convert.ToDecimal(r["PorcentajeReserva"]),
        ImagenPortada = r["ImagenPortada"] == DBNull.Value ? null : r["ImagenPortada"].ToString(),
        ImagenesAdicionales = r["ImagenesAdicionales"] == DBNull.Value ? null : r["ImagenesAdicionales"].ToString(),
        Disponible = Convert.ToBoolean(r["Disponible"]),
        PropietarioId = Convert.ToInt32(r["PropietarioId"]),
        TipoInmuebleId = Convert.ToInt32(r["TipoInmuebleId"]),
        Duenio = new Propietario { Nombre = r["PropNombre"].ToString() ?? "", Apellido = r["PropApellido"].ToString() ?? "" },
        Tipo = new TipoInmueble { Descripcion = r["TipoDesc"].ToString() ?? "" }
    };

    private Inmueble MapearInmuebleSimple(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        Nombre = r["Nombre"].ToString() ?? "",
        Descripcion = r["Descripcion"].ToString() ?? "",
        Direccion = r["Direccion"].ToString() ?? "",
        Cupo = Convert.ToInt32(r["Cupo"]),
        Latitud = r["Latitud"] != DBNull.Value ? Convert.ToDecimal(r["Latitud"]) : null,
        Longitud = r["Longitud"] != DBNull.Value ? Convert.ToDecimal(r["Longitud"]) : null,
        PrecioPorDia = Convert.ToDecimal(r["PrecioPorDia"]),
        PorcentajeReserva = Convert.ToDecimal(r["PorcentajeReserva"]),
        ImagenPortada = r["ImagenPortada"] == DBNull.Value ? null : r["ImagenPortada"].ToString(),
        ImagenesAdicionales = r["ImagenesAdicionales"] == DBNull.Value ? null : r["ImagenesAdicionales"].ToString(),
        Disponible = Convert.ToBoolean(r["Disponible"]),
        PropietarioId = Convert.ToInt32(r["PropietarioId"]),
        TipoInmuebleId = Convert.ToInt32(r["TipoInmuebleId"])
    };
}