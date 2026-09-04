using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;

namespace Inmobiliaria.Controllers;

public class ReservasController : Controller
{
    private readonly string _connectionString;

    public ReservasController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    private void CargarSelectLists()
    {
        var inquilinos = new List<SelectListItem>();
        var inmuebles = new List<SelectListItem>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            using var cmd = new MySqlCommand("SELECT Id, CONCAT(Apellido, ' ', Nombre) AS Info FROM Inquilinos", connection);
            using var r = cmd.ExecuteReader();
            while (r.Read()) inquilinos.Add(new SelectListItem(r["Info"].ToString(), r["Id"].ToString()));
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            using var cmd = new MySqlCommand("SELECT Id, CONCAT(Nombre, ' - ', Direccion) AS Info FROM Inmuebles WHERE Disponible = 1", connection);
            using var r = cmd.ExecuteReader();
            while (r.Read()) inmuebles.Add(new SelectListItem(r["Info"].ToString(), r["Id"].ToString()));
        }

        ViewBag.Inquilinos = inquilinos;
        ViewBag.Inmuebles = inmuebles;
    }

    public IActionResult Index()
    {
        var reservas = new List<Reserva>();
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT r.Id, r.FechaInicio, r.FechaFin, r.MontoDiario,
                                  i.Nombre AS InmuebleNombre, inq.Nombre AS InqNombre, inq.Apellido AS InqApellido
                           FROM Reservas r
                           INNER JOIN Inmuebles i ON r.InmuebleId = i.Id
                           INNER JOIN Inquilinos inq ON r.InquilinoId = inq.Id
                           ORDER BY r.FechaInicio DESC";
            using var cmd = new MySqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reservas.Add(new Reserva
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                    FechaFin = Convert.ToDateTime(reader["FechaFin"]),
                    MontoDiario = Convert.ToDecimal(reader["MontoDiario"]),
                    Inmueble = new Inmueble { Nombre = reader["InmuebleNombre"].ToString() ?? "" },
                    Inquilino = new Inquilino { Nombre = reader["InqNombre"].ToString() ?? "", Apellido = reader["InqApellido"].ToString() ?? "" }
                });
            }
        }
        return View(reservas);
    }

    public IActionResult Details(int id)
    {
        Reserva? reserva = null;
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT r.Id, r.FechaInicio, r.FechaFin, r.MontoDiario, r.InmuebleId, r.InquilinoId,
                                  i.Nombre AS InmuebleNombre, i.Direccion, i.PrecioPorDia,
                                  inq.Nombre AS InqNombre, inq.Apellido AS InqApellido, inq.Dni, inq.Telefono, inq.Email
                           FROM Reservas r
                           INNER JOIN Inmuebles i ON r.InmuebleId = i.Id
                           INNER JOIN Inquilinos inq ON r.InquilinoId = inq.Id
                           WHERE r.Id = @id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                reserva = new Reserva
                {
                    Id = Convert.ToInt32(r["Id"]),
                    FechaInicio = Convert.ToDateTime(r["FechaInicio"]),
                    FechaFin = Convert.ToDateTime(r["FechaFin"]),
                    MontoDiario = Convert.ToDecimal(r["MontoDiario"]),
                    InmuebleId = Convert.ToInt32(r["InmuebleId"]),
                    InquilinoId = Convert.ToInt32(r["InquilinoId"]),
                    Inmueble = new Inmueble { Nombre = r["InmuebleNombre"].ToString() ?? "", Direccion = r["Direccion"].ToString() ?? "" },
                    Inquilino = new Inquilino 
                    { 
                        Nombre = r["InqNombre"].ToString() ?? "", 
                        Apellido = r["InqApellido"].ToString() ?? "",
                        Dni = r["Dni"].ToString() ?? "",
                        Telefono = r["Telefono"].ToString() ?? "",
                        Email = r["Email"].ToString() ?? ""
                    }
                };
            }
        }
        if (reserva == null) return NotFound();
        return View(reserva);
    }

    public IActionResult Create()
    {
        CargarSelectLists();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Reserva reserva)
    {
        if (reserva.FechaFin <= reserva.FechaInicio)
        {
            ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la de inicio.");
        }

        if (!ModelState.IsValid)
        {
            CargarSelectLists();
            return View(reserva);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            // Verificación de solapamiento de fechas
            string checkSql = @"SELECT COUNT(1) FROM Reservas 
                               WHERE InmuebleId = @inmuebleId 
                               AND (@inicio <= FechaFin AND @fin >= FechaInicio)";
            using var checkCmd = new MySqlCommand(checkSql, connection);
            checkCmd.Parameters.AddWithValue("@inmuebleId", reserva.InmuebleId);
            checkCmd.Parameters.AddWithValue("@inicio", reserva.FechaInicio);
            checkCmd.Parameters.AddWithValue("@fin", reserva.FechaFin);

            if (Convert.ToInt64(checkCmd.ExecuteScalar()) > 0)
            {
                ModelState.AddModelError("", "El inmueble ya cuenta con una reserva activa en esas fechas.");
                CargarSelectLists();
                return View(reserva);
            }

            string sql = @"INSERT INTO Reservas (FechaInicio, FechaFin, MontoDiario, InmuebleId, InquilinoId)
                           VALUES (@inicio, @fin, @monto, @inmuebleId, @inquilinoId)";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@inicio", reserva.FechaInicio);
            cmd.Parameters.AddWithValue("@fin", reserva.FechaFin);
            cmd.Parameters.AddWithValue("@monto", reserva.MontoDiario);
            cmd.Parameters.AddWithValue("@inmuebleId", reserva.InmuebleId);
            cmd.Parameters.AddWithValue("@inquilinoId", reserva.InquilinoId);
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
            string sql = "DELETE FROM Reservas WHERE Id = @id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
        return RedirectToAction(nameof(Index));
    }
}