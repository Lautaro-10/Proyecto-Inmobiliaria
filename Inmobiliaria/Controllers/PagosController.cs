using System.Security.Claims;
using Inmobiliaria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Inmobiliaria.Controllers;

[Authorize]
public class PagosController : Controller
{
    private readonly string _connectionString;

    public PagosController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public IActionResult Index(int reservaId)
    {
        var reserva = ObtenerReserva(reservaId);
        if (reserva == null) return NotFound();

        var pagos = new List<Pago>();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"SELECT Id, ReservaId, Monto, Concepto, Estado, FechaPago,
                                     FechaCreacion, CreadoPorUsuarioId, ModificadoPorUsuarioId,
                                     FechaModificacion, uc.Email AS CreadorEmail, um.Email AS ModificadorEmail
                                 FROM Pagos p
                                 LEFT JOIN Usuarios uc ON uc.Id = p.CreadoPorUsuarioId
                                 LEFT JOIN Usuarios um ON um.Id = p.ModificadoPorUsuarioId
                             WHERE p.ReservaId = @reservaId
                             ORDER BY FechaPago DESC, Id DESC";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservaId", reservaId);
        using var reader = command.ExecuteReader();
        while (reader.Read()) pagos.Add(MapearPago(reader));

        ViewBag.Reserva = reserva;
        return View(pagos);
    }

    public IActionResult Create(int reservaId)
    {
        var reserva = ObtenerReserva(reservaId);
        if (reserva == null) return NotFound();

        ViewBag.Reserva = reserva;
        return View(new Pago
        {
            ReservaId = reservaId,
            FechaPago = DateTime.Today,
            Estado = "Realizado"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Pago pago)
    {
        var reserva = ObtenerReserva(pago.ReservaId);
        if (reserva == null) return NotFound();

        if (pago.Monto <= 0)
            ModelState.AddModelError(nameof(Pago.Monto), "El importe debe ser mayor que cero.");

        if (!pago.FechaPago.HasValue)
            ModelState.AddModelError(nameof(Pago.FechaPago), "La fecha de pago es obligatoria.");

        if (!ModelState.IsValid)
        {
            ViewBag.Reserva = reserva;
            return View(pago);
        }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"INSERT INTO Pagos
                             (ReservaId, Monto, Concepto, Estado, FechaPago, CreadoPorUsuarioId)
                             VALUES (@reservaId, @monto, @concepto, 'Realizado', @fechaPago, @usuarioId)";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservaId", pago.ReservaId);
        command.Parameters.AddWithValue("@monto", pago.Monto);
        command.Parameters.AddWithValue("@concepto", pago.Concepto);
        command.Parameters.AddWithValue("@fechaPago", pago.FechaPago ?? throw new InvalidOperationException("La fecha de pago es obligatoria."));
        command.Parameters.AddWithValue("@usuarioId", UsuarioActualId());
        command.ExecuteNonQuery();

        return RedirectToAction(nameof(Index), new { reservaId = pago.ReservaId });
    }

    public IActionResult Edit(int id)
    {
        var pago = ObtenerPago(id);
        if (pago == null) return NotFound();

        ViewBag.Reserva = ObtenerReserva(pago.ReservaId);
        return View(pago);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Pago pago)
    {
        if (id != pago.Id) return BadRequest();

        var pagoExistente = ObtenerPago(id);
        if (pagoExistente == null) return NotFound();

        ModelState.Remove(nameof(Pago.Monto));
        ModelState.Remove(nameof(Pago.FechaPago));
        ModelState.Remove(nameof(Pago.Estado));

        if (!ModelState.IsValid)
        {
            ViewBag.Reserva = ObtenerReserva(pagoExistente.ReservaId);
            return View(pago);
        }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"UPDATE Pagos
                             SET Concepto = @concepto,
                                 ModificadoPorUsuarioId = @usuarioId,
                                 FechaModificacion = UTC_TIMESTAMP()
                             WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@concepto", pago.Concepto);
        command.Parameters.AddWithValue("@usuarioId", UsuarioActualId());
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();

        return RedirectToAction(nameof(Index), new { reservaId = pagoExistente.ReservaId });
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Anular(int id)
    {
        var pago = ObtenerPago(id);
        if (pago == null) return NotFound();

        ViewBag.Reserva = ObtenerReserva(pago.ReservaId);
        return View(pago);
    }

    [HttpPost]
    [ActionName("Anular")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public IActionResult AnularConfirmado(int id)
    {
        var pago = ObtenerPago(id);
        if (pago == null) return NotFound();

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"UPDATE Pagos
                             SET Estado = 'Anulado',
                                 ModificadoPorUsuarioId = @usuarioId,
                                 FechaModificacion = UTC_TIMESTAMP()
                             WHERE Id = @id AND Estado <> 'Anulado'";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@usuarioId", UsuarioActualId());
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();

        return RedirectToAction(nameof(Index), new { reservaId = pago.ReservaId });
    }

    private Pago? ObtenerPago(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"SELECT Id, ReservaId, Monto, Concepto, Estado, FechaPago,
                                     FechaCreacion, CreadoPorUsuarioId, ModificadoPorUsuarioId,
                                     FechaModificacion, uc.Email AS CreadorEmail, um.Email AS ModificadorEmail
                                 FROM Pagos p
                                 LEFT JOIN Usuarios uc ON uc.Id = p.CreadoPorUsuarioId
                                 LEFT JOIN Usuarios um ON um.Id = p.ModificadoPorUsuarioId
                                 WHERE p.Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapearPago(reader) : null;
    }

    private Reserva? ObtenerReserva(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"SELECT r.Id, r.FechaInicio, r.FechaFin, r.MontoDiario,
                                    i.Nombre AS InmuebleNombre,
                                    CONCAT(inq.Apellido, ', ', inq.Nombre) AS InquilinoNombre
                             FROM Reservas r
                             INNER JOIN Inmuebles i ON i.Id = r.InmuebleId
                             INNER JOIN Inquilinos inq ON inq.Id = r.InquilinoId
                             WHERE r.Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return new Reserva
        {
            Id = reader.GetInt32("Id"),
            FechaInicio = reader.GetDateTime("FechaInicio"),
            FechaFin = reader.GetDateTime("FechaFin"),
            MontoDiario = reader.GetDecimal("MontoDiario"),
            Inmueble = new Inmueble { Nombre = reader.GetString("InmuebleNombre") },
            Inquilino = new Inquilino { Nombre = reader.GetString("InquilinoNombre") }
        };
    }

    private static Pago MapearPago(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32("Id"),
        ReservaId = reader.GetInt32("ReservaId"),
        Monto = reader.GetDecimal("Monto"),
        Concepto = reader.GetString("Concepto"),
        Estado = reader.GetString("Estado"),
        FechaPago = reader.IsDBNull(reader.GetOrdinal("FechaPago")) ? null : reader.GetDateTime("FechaPago"),
        FechaCreacion = reader.GetDateTime("FechaCreacion"),
        CreadoPorUsuarioId = reader.IsDBNull(reader.GetOrdinal("CreadoPorUsuarioId")) ? null : reader.GetInt32("CreadoPorUsuarioId"),
        ModificadoPorUsuarioId = reader.IsDBNull(reader.GetOrdinal("ModificadoPorUsuarioId")) ? null : reader.GetInt32("ModificadoPorUsuarioId"),
        FechaModificacion = reader.IsDBNull(reader.GetOrdinal("FechaModificacion")) ? null : reader.GetDateTime("FechaModificacion"),
        CreadoPorUsuario = reader.IsDBNull(reader.GetOrdinal("CreadorEmail")) ? null : new Usuario { Email = reader.GetString("CreadorEmail") },
        ModificadoPorUsuario = reader.IsDBNull(reader.GetOrdinal("ModificadorEmail")) ? null : new Usuario { Email = reader.GetString("ModificadorEmail") }
    };

    private int UsuarioActualId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
