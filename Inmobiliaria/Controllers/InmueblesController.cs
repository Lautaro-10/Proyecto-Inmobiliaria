using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;

namespace Inmobiliaria.Controllers;

[Authorize]
public class InmueblesController : Controller
{
    private readonly string _connectionString;

    public InmueblesController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
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
    public IActionResult Create(Inmueble inmueble)
    {
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
    public IActionResult Edit(int id, Inmueble inmueble)
    {
        if (id != inmueble.Id || !ModelState.IsValid)
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
            cmd.ExecuteNonQuery();
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
                var inmueble = ObtenerPorId(id);
                if (inmueble == null) return NotFound();
                ModelState.AddModelError(string.Empty, "No se puede eliminar este inmueble porque tiene reservas asociadas.");
                return View("Delete", inmueble);
            }
        }
        return RedirectToAction(nameof(Index));
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