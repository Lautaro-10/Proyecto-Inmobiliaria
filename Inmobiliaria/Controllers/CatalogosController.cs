using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Inmobiliaria.Controllers;

[Authorize]
public class CatalogosController : Controller
{
    private readonly string _connectionString;

    public CatalogosController(IConfiguration configuration) =>
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

    [HttpGet]
    public IActionResult Buscar(string entidad, string? q, DateTime? inicio = null, DateTime? fin = null)
    {
        var termino = q?.Trim() ?? string.Empty;
        if (termino.Length < 2) return Json(Array.Empty<object>());

        var sql = entidad switch
        {
            "propietarios" => @"SELECT Id AS Id, CONCAT(Apellido, ', ', Nombre, ' · DNI ', Dni) AS Texto
                                FROM Propietarios
                                WHERE Nombre LIKE CONCAT('%', @termino, '%')
                                   OR Apellido LIKE CONCAT('%', @termino, '%')
                                   OR Dni LIKE CONCAT('%', @termino, '%')
                                ORDER BY Apellido, Nombre LIMIT 20",
            "inquilinos" => @"SELECT Id AS Id, CONCAT(Apellido, ', ', Nombre, ' · DNI ', Dni) AS Texto
                              FROM Inquilinos
                              WHERE Nombre LIKE CONCAT('%', @termino, '%')
                                 OR Apellido LIKE CONCAT('%', @termino, '%')
                                 OR Dni LIKE CONCAT('%', @termino, '%')
                              ORDER BY Apellido, Nombre LIMIT 20",
            "tipos" => @"SELECT Id AS Id, Descripcion AS Texto
                         FROM TiposInmueble
                         WHERE Descripcion LIKE CONCAT('%', @termino, '%')
                         ORDER BY Descripcion LIMIT 20",
            "inmuebles" => @"SELECT i.Id, CONCAT(i.Nombre, ' · ', i.Direccion) AS Texto
                             FROM Inmuebles i
                             WHERE i.Disponible = 1
                               AND (i.Nombre LIKE CONCAT('%', @termino, '%')
                                    OR i.Direccion LIKE CONCAT('%', @termino, '%'))
                               AND (@inicio IS NULL OR @fin IS NULL OR NOT EXISTS (
                                   SELECT 1 FROM Reservas r
                                   WHERE r.InmuebleId = i.Id
                                     AND r.Estado IN ('Activa', 'Pendiente')
                                     AND @inicio <= r.FechaFin AND @fin >= r.FechaInicio))
                             ORDER BY i.Nombre LIMIT 20",
            _ => null
        };
        if (sql is null) return BadRequest(new { error = "Entidad de búsqueda inválida." });

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@termino", termino);
        if (entidad == "inmuebles")
        {
            command.Parameters.AddWithValue("@inicio", (object?)inicio?.Date ?? DBNull.Value);
            command.Parameters.AddWithValue("@fin", (object?)fin?.Date ?? DBNull.Value);
        }

        var resultados = new List<object>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            resultados.Add(new { id = reader.GetInt32("Id"), text = reader.GetString("Texto") });
        return Json(resultados);
    }
}
