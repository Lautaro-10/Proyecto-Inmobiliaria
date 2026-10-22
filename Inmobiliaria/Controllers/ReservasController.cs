using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Inmobiliaria.Controllers;

[Authorize]
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

    public IActionResult Index(string? buscar, string? estado, int pagina = 1)
    {
        pagina = Math.Max(1, pagina);
        var reservas = new List<Reserva>();
        var total = 0;
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            const string filtro = @"WHERE (@buscar = '' OR i.Nombre LIKE CONCAT('%', @buscar, '%')
                                         OR inq.Nombre LIKE CONCAT('%', @buscar, '%')
                                         OR inq.Apellido LIKE CONCAT('%', @buscar, '%'))
                                   AND (@estado = '' OR r.Estado = @estado)";
            using var totalCmd = new MySqlCommand($@"SELECT COUNT(*) FROM Reservas r
                                                     INNER JOIN Inmuebles i ON i.Id = r.InmuebleId
                                                     INNER JOIN Inquilinos inq ON inq.Id = r.InquilinoId
                                                     {filtro}", connection);
            totalCmd.Parameters.AddWithValue("@buscar", buscar ?? "");
            totalCmd.Parameters.AddWithValue("@estado", estado ?? "");
            total = Convert.ToInt32(totalCmd.ExecuteScalar());
            string sql = @"SELECT r.Id, r.FechaInicio, r.FechaFin, r.MontoDiario,
                                  r.Estado, r.FechaCreacion, r.FechaFinOriginal,
                                  r.FechaTerminacion, r.CreadoPorUsuarioId,
                                  r.TerminadoPorUsuarioId, i.Nombre AS InmuebleNombre,
                                  inq.Nombre AS InqNombre, inq.Apellido AS InqApellido
                           FROM Reservas r
                           INNER JOIN Inmuebles i ON i.Id = r.InmuebleId
                           INNER JOIN Inquilinos inq ON inq.Id = r.InquilinoId
                           " + filtro + @"
                           ORDER BY r.FechaInicio DESC
                           LIMIT @limite OFFSET @offset";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@buscar", buscar ?? "");
            cmd.Parameters.AddWithValue("@estado", estado ?? "");
            cmd.Parameters.AddWithValue("@limite", 10);
            cmd.Parameters.AddWithValue("@offset", (pagina - 1) * 10);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reservas.Add(new Reserva
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                    FechaFin = Convert.ToDateTime(reader["FechaFin"]),
                    MontoDiario = Convert.ToDecimal(reader["MontoDiario"]),
                    Estado = reader["Estado"].ToString() ?? "Pendiente",
                    FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"]),
                    FechaFinOriginal = reader["FechaFinOriginal"] == DBNull.Value ? null : Convert.ToDateTime(reader["FechaFinOriginal"]),
                    FechaTerminacion = reader["FechaTerminacion"] == DBNull.Value ? null : Convert.ToDateTime(reader["FechaTerminacion"]),
                    CreadoPorUsuarioId = reader["CreadoPorUsuarioId"] == DBNull.Value ? null : Convert.ToInt32(reader["CreadoPorUsuarioId"]),
                    TerminadoPorUsuarioId = reader["TerminadoPorUsuarioId"] == DBNull.Value ? null : Convert.ToInt32(reader["TerminadoPorUsuarioId"]),
                    Inmueble = new Inmueble { Nombre = reader["InmuebleNombre"].ToString() ?? "" },
                    Inquilino = new Inquilino { Nombre = reader["InqNombre"].ToString() ?? "", Apellido = reader["InqApellido"].ToString() ?? "" }
                });
            }
        }
        return View(new ReservaListadoViewModel
        {
            Reservas = reservas,
            Buscar = buscar,
            Estado = estado,
            Pagina = pagina,
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / 10d))
        });
    }

    public IActionResult Details(int id)
    {
        Reserva? reserva = null;
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT r.Id, r.FechaInicio, r.FechaFin, r.MontoDiario, r.InmuebleId, r.InquilinoId, r.Estado, r.FechaCreacion, r.FechaFinOriginal, r.FechaTerminacion, r.CreadoPorUsuarioId, r.TerminadoPorUsuarioId,
                                  i.Nombre AS InmuebleNombre, i.Direccion, i.PrecioPorDia,
                                      inq.Nombre AS InqNombre, inq.Apellido AS InqApellido, inq.Dni, inq.Telefono, inq.Email,
                                      uc.Email AS CreadorEmail, ut.Email AS TerminadorEmail
                           FROM Reservas r
                           INNER JOIN Inmuebles i ON r.InmuebleId = i.Id
                           INNER JOIN Inquilinos inq ON r.InquilinoId = inq.Id
                                  LEFT JOIN Usuarios uc ON uc.Id = r.CreadoPorUsuarioId
                                  LEFT JOIN Usuarios ut ON ut.Id = r.TerminadoPorUsuarioId
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
                    Estado = r["Estado"].ToString() ?? "Pendiente",
                    FechaCreacion = Convert.ToDateTime(r["FechaCreacion"]),
                    FechaFinOriginal = r["FechaFinOriginal"] == DBNull.Value ? null : Convert.ToDateTime(r["FechaFinOriginal"]),
                    FechaTerminacion = r["FechaTerminacion"] == DBNull.Value ? null : Convert.ToDateTime(r["FechaTerminacion"]),
                    CreadoPorUsuarioId = r["CreadoPorUsuarioId"] == DBNull.Value ? null : Convert.ToInt32(r["CreadoPorUsuarioId"]),
                    TerminadoPorUsuarioId = r["TerminadoPorUsuarioId"] == DBNull.Value ? null : Convert.ToInt32(r["TerminadoPorUsuarioId"]),
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

            string checkSql = @"SELECT COUNT(1) FROM Reservas
                               WHERE InmuebleId = @inmuebleId
                               AND Estado IN ('Activa', 'Pendiente')
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

            string sql = @"INSERT INTO Reservas (FechaInicio, FechaFin, MontoDiario, InmuebleId, InquilinoId, Estado, CreadoPorUsuarioId)
                           VALUES (@inicio, @fin, @monto, @inmuebleId, @inquilinoId, 'Activa', @usuarioId)";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@inicio", reserva.FechaInicio);
            cmd.Parameters.AddWithValue("@fin", reserva.FechaFin);
            cmd.Parameters.AddWithValue("@monto", reserva.MontoDiario);
            cmd.Parameters.AddWithValue("@inmuebleId", reserva.InmuebleId);
            cmd.Parameters.AddWithValue("@inquilinoId", reserva.InquilinoId);
            cmd.Parameters.AddWithValue("@usuarioId", UsuarioActualId());
            cmd.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Disponibles(DisponibilidadViewModel modelo)
    {
        if (modelo.FechaFin <= modelo.FechaInicio)
        {
            ModelState.AddModelError(nameof(modelo.FechaFin), "La fecha de fin debe ser posterior a la de inicio.");
            CargarTipos();
            return View(modelo);
        }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"SELECT i.*, p.Nombre AS PropNombre, p.Apellido AS PropApellido,
                                    t.Descripcion AS TipoDesc
                             FROM Inmuebles i
                             INNER JOIN Propietarios p ON p.Id = i.PropietarioId
                             INNER JOIN TiposInmueble t ON t.Id = i.TipoInmuebleId
                             WHERE i.Disponible = 1
                               AND (@tipoId IS NULL OR i.TipoInmuebleId = @tipoId)
                               AND (@cupo IS NULL OR i.Cupo >= @cupo)
                               AND NOT EXISTS (
                                   SELECT 1 FROM Reservas r
                                   WHERE r.InmuebleId = i.Id
                                     AND r.Estado IN ('Activa', 'Pendiente')
                                     AND @inicio <= r.FechaFin AND @fin >= r.FechaInicio)
                             ORDER BY i.Nombre";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@inicio", modelo.FechaInicio.Date);
        command.Parameters.AddWithValue("@fin", modelo.FechaFin.Date);
        command.Parameters.AddWithValue("@tipoId", (object?)modelo.TipoInmuebleId ?? DBNull.Value);
        command.Parameters.AddWithValue("@cupo", (object?)modelo.CupoMinimo ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        while (reader.Read()) modelo.Inmuebles.Add(MapearInmueble(reader));
        CargarTipos();
        return View(modelo);
    }

    public IActionResult Terminar(int id)
    {
        var reserva = ObtenerReserva(id);
        if (reserva == null || reserva.Estado == "Terminada") return NotFound();
        return View(CalcularTerminacion(reserva, DateTime.Today));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Terminar(TerminarReservaViewModel modelo)
    {
        var reserva = ObtenerReserva(modelo.ReservaId);
        if (reserva == null) return NotFound();
        var calculo = CalcularTerminacion(reserva, modelo.FechaTerminacion);
        if (modelo.FechaTerminacion.Date < reserva.FechaInicio.Date || modelo.FechaTerminacion.Date >= reserva.FechaFin.Date)
            ModelState.AddModelError(nameof(modelo.FechaTerminacion), "La fecha debe estar dentro del período y antes de la fecha original de fin.");
        if (!modelo.ConfirmarPago)
            ModelState.AddModelError(nameof(modelo.ConfirmarPago), "Debe confirmar el pago de la multa para finalizar.");
        calculo.Concepto = modelo.Concepto;
        if (!ModelState.IsValid) return View(calculo);

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            const string update = @"UPDATE Reservas
                                    SET Estado = 'Terminada', FechaTerminacion = @fecha,
                                        FechaFinOriginal = FechaFin, FechaFin = @fecha,
                                        TerminadoPorUsuarioId = @usuarioId
                                    WHERE Id = @id AND Estado IN ('Activa', 'Pendiente')";
            using var updateCommand = new MySqlCommand(update, connection, transaction);
            updateCommand.Parameters.AddWithValue("@fecha", modelo.FechaTerminacion.Date);
            updateCommand.Parameters.AddWithValue("@usuarioId", UsuarioActualId());
            updateCommand.Parameters.AddWithValue("@id", reserva.Id);
            if (updateCommand.ExecuteNonQuery() != 1) throw new InvalidOperationException("La reserva ya no está activa.");

            const string payment = @"INSERT INTO Pagos
                                     (ReservaId, Monto, Concepto, Estado, FechaPago, CreadoPorUsuarioId)
                                     VALUES (@reservaId, @monto, @concepto, 'Realizado', UTC_TIMESTAMP(), @usuarioId)";
            using var paymentCommand = new MySqlCommand(payment, connection, transaction);
            paymentCommand.Parameters.AddWithValue("@reservaId", reserva.Id);
            paymentCommand.Parameters.AddWithValue("@monto", calculo.Multa);
            paymentCommand.Parameters.AddWithValue("@concepto", calculo.Concepto);
            paymentCommand.Parameters.AddWithValue("@usuarioId", UsuarioActualId());
            paymentCommand.ExecuteNonQuery();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        return RedirectToAction(nameof(Details), new { id = reserva.Id });
    }

    public IActionResult Renovar(int id)
    {
        var reserva = ObtenerReserva(id);
        if (reserva == null) return NotFound();
        return View(new RenovarReservaViewModel
        {
            ReservaId = id, FechaInicio = reserva.FechaFin.AddDays(1), FechaFin = reserva.FechaFin.AddDays(2),
            MontoDiario = reserva.MontoDiario, Reserva = reserva
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Renovar(RenovarReservaViewModel modelo)
    {
        var reserva = ObtenerReserva(modelo.ReservaId);
        if (reserva == null) return NotFound();
        if (modelo.FechaFin <= modelo.FechaInicio)
            ModelState.AddModelError(nameof(modelo.FechaFin), "La fecha de fin debe ser posterior a la de inicio.");
        if (modelo.MontoDiario <= 0)
            ModelState.AddModelError(nameof(modelo.MontoDiario), "El monto debe ser mayor que cero.");
        if (!ModelState.IsValid) { modelo.Reserva = reserva; return View(modelo); }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        if (!InmuebleDisponible(connection, reserva.InmuebleId, modelo.FechaInicio, modelo.FechaFin))
        {
            ModelState.AddModelError(string.Empty, "El inmueble ya está ocupado en esas fechas.");
            modelo.Reserva = reserva;
            return View(modelo);
        }
        const string sql = @"INSERT INTO Reservas
                             (FechaInicio, FechaFin, MontoDiario, InmuebleId, InquilinoId, Estado, CreadoPorUsuarioId)
                             VALUES (@inicio, @fin, @monto, @inmuebleId, @inquilinoId, 'Activa', @usuarioId)";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@inicio", modelo.FechaInicio.Date);
        command.Parameters.AddWithValue("@fin", modelo.FechaFin.Date);
        command.Parameters.AddWithValue("@monto", modelo.MontoDiario);
        command.Parameters.AddWithValue("@inmuebleId", reserva.InmuebleId);
        command.Parameters.AddWithValue("@inquilinoId", reserva.InquilinoId);
        command.Parameters.AddWithValue("@usuarioId", UsuarioActualId());
        command.ExecuteNonQuery();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Delete(int id) => Details(id);

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = "Administrador")]
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

    public IActionResult Informes(string tipo = "vigentes", int dias = 30, int? propietarioId = null)
    {
        ViewBag.Tipo = tipo;
        ViewBag.Dias = Math.Max(1, dias);
        ViewBag.PropietarioId = propietarioId;
        CargarPropietarios();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        if (tipo == "vigentes" || tipo == "proximas")
        {
            var reservas = new List<InformeReservaViewModel>();
            var filtro = tipo == "vigentes"
                ? "CURDATE() BETWEEN r.FechaInicio AND r.FechaFin AND r.Estado IN ('Activa', 'Pendiente')"
                : "r.FechaFin BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL @dias DAY) AND r.Estado IN ('Activa', 'Pendiente')";
            using var command = new MySqlCommand($@"SELECT r.Id, i.Nombre AS Inmueble,
                                                            CONCAT(inq.Apellido, ', ', inq.Nombre) AS Inquilino,
                                                            r.FechaInicio, r.FechaFin, r.Estado
                                                     FROM Reservas r
                                                     INNER JOIN Inmuebles i ON i.Id = r.InmuebleId
                                                     INNER JOIN Inquilinos inq ON inq.Id = r.InquilinoId
                                                     WHERE {filtro} ORDER BY r.FechaFin", connection);
            command.Parameters.AddWithValue("@dias", Math.Max(1, dias));
            using var reader = command.ExecuteReader();
            while (reader.Read()) reservas.Add(new InformeReservaViewModel
            {
                Id = reader.GetInt32("Id"), Inmueble = reader.GetString("Inmueble"), Inquilino = reader.GetString("Inquilino"),
                FechaInicio = reader.GetDateTime("FechaInicio"), FechaFin = reader.GetDateTime("FechaFin"), Estado = reader.GetString("Estado")
            });
            return View(reservas);
        }

        var inmuebles = new List<InformeInmuebleViewModel>();
        var condicion = tipo == "mas-reservados"
            ? "r.FechaInicio >= DATE_SUB(CURDATE(), INTERVAL 365 DAY)"
            : "r.FechaInicio >= DATE_SUB(CURDATE(), INTERVAL @dias DAY)";
        var cierre = tipo == "mas-reservados"
            ? "ORDER BY CantidadReservas DESC"
            : "HAVING COUNT(r.Id) = 0 ORDER BY i.Nombre";
        var filtroPropietario = tipo == "por-propietario" ? "WHERE (@propietarioId IS NULL OR i.PropietarioId = @propietarioId)" : "";
        using var inmuebleCommand = new MySqlCommand($@"SELECT i.Id, i.Nombre,
                                                               CONCAT(p.Apellido, ', ', p.Nombre) AS Propietario,
                                                               COUNT(r.Id) AS CantidadReservas,
                                                               MAX(r.FechaInicio) AS UltimaReserva
                                                        FROM Inmuebles i
                                                        INNER JOIN Propietarios p ON p.Id = i.PropietarioId
                                                        LEFT JOIN Reservas r ON r.InmuebleId = i.Id AND {condicion}
                                                        {filtroPropietario}
                                                        GROUP BY i.Id, i.Nombre, p.Apellido, p.Nombre
                                                        {(tipo == "por-propietario" ? "ORDER BY i.Nombre" : cierre)}", connection);
        inmuebleCommand.Parameters.AddWithValue("@dias", Math.Max(1, dias));
        inmuebleCommand.Parameters.AddWithValue("@propietarioId", (object?)propietarioId ?? DBNull.Value);
        using var inmuebleReader = inmuebleCommand.ExecuteReader();
        while (inmuebleReader.Read()) inmuebles.Add(new InformeInmuebleViewModel
        {
            Id = inmuebleReader.GetInt32("Id"), Nombre = inmuebleReader.GetString("Nombre"),
            Propietario = inmuebleReader.GetString("Propietario"), CantidadReservas = inmuebleReader.GetInt32("CantidadReservas"),
            UltimaReserva = inmuebleReader.IsDBNull(inmuebleReader.GetOrdinal("UltimaReserva")) ? null : inmuebleReader.GetDateTime("UltimaReserva")
        });
        return View(inmuebles);
    }

    private TerminarReservaViewModel CalcularTerminacion(Reserva reserva, DateTime fecha)
    {
        var finOriginal = reserva.FechaFinOriginal ?? reserva.FechaFin;
        var diasTotales = Math.Max(1, (finOriginal.Date - reserva.FechaInicio.Date).Days);
        var diasUsados = Math.Max(0, (fecha.Date - reserva.FechaInicio.Date).Days);
        var diasRestantes = Math.Max(0, diasTotales - diasUsados);
        var porcentaje = diasUsados < diasTotales / 2m ? 0.50m : 0.25m;
        return new TerminarReservaViewModel
        {
            ReservaId = reserva.Id, FechaTerminacion = fecha, DiasRestantes = diasRestantes,
            MontoDiario = reserva.MontoDiario, Multa = diasRestantes * reserva.MontoDiario * porcentaje,
            Reserva = reserva
        };
    }

    private bool InmuebleDisponible(MySqlConnection connection, int inmuebleId, DateTime inicio, DateTime fin)
    {
        const string sql = @"SELECT COUNT(*) FROM Reservas
                             WHERE InmuebleId = @inmuebleId AND Estado IN ('Activa', 'Pendiente')
                               AND (@inicio <= FechaFin AND @fin >= FechaInicio)";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@inmuebleId", inmuebleId);
        command.Parameters.AddWithValue("@inicio", inicio.Date);
        command.Parameters.AddWithValue("@fin", fin.Date);
        return Convert.ToInt32(command.ExecuteScalar()) == 0;
    }

    private Reserva? ObtenerReserva(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = @"SELECT r.*, i.Nombre AS InmuebleNombre, i.Direccion,
                                    inq.Nombre AS InqNombre, inq.Apellido AS InqApellido,
                                    inq.Dni, inq.Telefono, inq.Email
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
            Id = reader.GetInt32("Id"), InmuebleId = reader.GetInt32("InmuebleId"), InquilinoId = reader.GetInt32("InquilinoId"),
            FechaInicio = reader.GetDateTime("FechaInicio"), FechaFin = reader.GetDateTime("FechaFin"), MontoDiario = reader.GetDecimal("MontoDiario"),
            Estado = reader.GetString("Estado"), FechaCreacion = reader.GetDateTime("FechaCreacion"),
            FechaFinOriginal = reader.IsDBNull(reader.GetOrdinal("FechaFinOriginal")) ? null : reader.GetDateTime("FechaFinOriginal"),
            FechaTerminacion = reader.IsDBNull(reader.GetOrdinal("FechaTerminacion")) ? null : reader.GetDateTime("FechaTerminacion"),
            CreadoPorUsuarioId = reader.IsDBNull(reader.GetOrdinal("CreadoPorUsuarioId")) ? null : reader.GetInt32("CreadoPorUsuarioId"),
            TerminadoPorUsuarioId = reader.IsDBNull(reader.GetOrdinal("TerminadoPorUsuarioId")) ? null : reader.GetInt32("TerminadoPorUsuarioId"),
            CreadoPorUsuario = reader.IsDBNull(reader.GetOrdinal("CreadorEmail")) ? null : new Usuario { Email = reader.GetString("CreadorEmail") },
            TerminadoPorUsuario = reader.IsDBNull(reader.GetOrdinal("TerminadorEmail")) ? null : new Usuario { Email = reader.GetString("TerminadorEmail") },
            Inmueble = new Inmueble { Nombre = reader.GetString("InmuebleNombre"), Direccion = reader.GetString("Direccion") },
            Inquilino = new Inquilino { Nombre = reader.GetString("InqNombre"), Apellido = reader.GetString("InqApellido"), Dni = reader.GetString("Dni"), Telefono = reader.GetString("Telefono"), Email = reader.GetString("Email") }
        };
    }

    private static Inmueble MapearInmueble(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32("Id"), Nombre = reader.GetString("Nombre"), Descripcion = reader.GetString("Descripcion"),
        Direccion = reader.GetString("Direccion"), Cupo = reader.GetInt32("Cupo"), PrecioPorDia = reader.GetDecimal("PrecioPorDia"),
        PorcentajeReserva = reader.GetDecimal("PorcentajeReserva"), Disponible = reader.GetBoolean("Disponible"),
        PropietarioId = reader.GetInt32("PropietarioId"), TipoInmuebleId = reader.GetInt32("TipoInmuebleId"),
        Duenio = new Propietario { Nombre = reader.GetString("PropNombre"), Apellido = reader.GetString("PropApellido") },
        Tipo = new TipoInmueble { Descripcion = reader.GetString("TipoDesc") }
    };

    private void CargarTipos()
    {
        var tipos = new List<SelectListItem>();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand("SELECT Id, Descripcion FROM TiposInmueble ORDER BY Descripcion", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read()) tipos.Add(new SelectListItem(reader.GetString("Descripcion"), reader.GetString("Id")));
        ViewBag.Tipos = tipos;
    }

    private void CargarPropietarios()
    {
        var propietarios = new List<SelectListItem>();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand("SELECT Id, CONCAT(Apellido, ' ', Nombre) AS NombreCompleto FROM Propietarios ORDER BY Apellido, Nombre", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read()) propietarios.Add(new SelectListItem(reader.GetString("NombreCompleto"), reader.GetString("Id")));
        ViewBag.Propietarios = propietarios;
    }

    private int UsuarioActualId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}