using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;

namespace Inmobiliaria.Controllers;

public class InmueblesController : Controller
{
    private readonly string _connectionString;

    public InmueblesController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    private void CargarSelectLists()
    {
        var propietarios = new List<SelectListItem>();
        var tipos = new List<SelectListItem>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            using var cmdProp = new MySqlCommand("SELECT Id, CONCAT(Apellido, ' ', Nombre) AS NombreCompleto FROM Propietarios", connection);
            using var rProp = cmdProp.ExecuteReader();
            while (rProp.Read())
            {
                propietarios.Add(new SelectListItem(rProp["NombreCompleto"].ToString(), rProp["Id"].ToString()));
            }
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            using var cmdTipo = new MySqlCommand("SELECT Id, Descripcion FROM TiposInmueble", connection);
            using var rTipo = cmdTipo.ExecuteReader();
            while (rTipo.Read())
            {
                tipos.Add(new SelectListItem(rTipo["Descripcion"].ToString(), rTipo["Id"].ToString()));
            }
        }

        ViewBag.Propietarios = propietarios;
        ViewBag.TiposInmueble = tipos;
    }

    public IActionResult Index()
    {
        var inmuebles = new List<Inmueble>();
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT i.*, p.Nombre AS PropNombre, p.Apellido AS PropApellido, t.Descripcion AS TipoDesc
                           FROM Inmuebles i
                           INNER JOIN Propietarios p ON i.PropietarioId = p.Id
                           INNER JOIN TiposInmueble t ON i.TipoInmuebleId = t.Id";
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                inmuebles.Add(MapearInmueble(reader));
            }
        }
        return View(inmuebles);
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
        CargarSelectLists();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Inmueble inmueble)
    {
        if (!ModelState.IsValid)
        {
            CargarSelectLists();
            return View(inmueble);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"INSERT INTO Inmuebles 
                (Nombre, Descripcion, Direccion, Cupo, Latitud, Longitud, PrecioPorDia, PorcentajeReserva, Disponible, PropietarioId, TipoInmuebleId)
                VALUES (@nombre, @desc, @dir, @cupo, @lat, @lng, @precio, @porc, @disp, @propId, @tipoId)";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@nombre", inmueble.Nombre);
            cmd.Parameters.AddWithValue("@desc", inmueble.Descripcion);
            cmd.Parameters.AddWithValue("@dir", inmueble.Direccion);
            cmd.Parameters.AddWithValue("@cupo", inmueble.Cupo);
            cmd.Parameters.AddWithValue("@lat", (object?)inmueble.Latitud ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lng", (object?)inmueble.Longitud ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@precio", inmueble.PrecioPorDia);
            cmd.Parameters.AddWithValue("@porc", inmueble.PorcentajeReserva);
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
        CargarSelectLists();
        return View(inmueble);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Inmueble inmueble)
    {
        if (id != inmueble.Id || !ModelState.IsValid)
        {
            CargarSelectLists();
            return View(inmueble);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"UPDATE Inmuebles SET 
                Nombre = @nombre, Descripcion = @desc, Direccion = @dir, Cupo = @cupo, 
                Latitud = @lat, Longitud = @lng, PrecioPorDia = @precio, PorcentajeReserva = @porc, 
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
            cmd.Parameters.AddWithValue("@disp", inmueble.Disponible ? 1 : 0);
            cmd.Parameters.AddWithValue("@propId", inmueble.PropietarioId);
            cmd.Parameters.AddWithValue("@tipoId", inmueble.TipoInmuebleId);
            cmd.ExecuteNonQuery();
        }
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id) => Details(id);

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "DELETE FROM Inmuebles WHERE Id = @id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
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
        Disponible = Convert.ToBoolean(r["Disponible"]),
        PropietarioId = Convert.ToInt32(r["PropietarioId"]),
        TipoInmuebleId = Convert.ToInt32(r["TipoInmuebleId"])
    };
}