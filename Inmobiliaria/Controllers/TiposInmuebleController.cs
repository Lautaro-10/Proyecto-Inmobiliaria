using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;
namespace Inmobiliaria.Controllers;

[Authorize]
public class TiposInmuebleController : Controller
{
    private readonly string _connectionString;

    public TiposInmuebleController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    public IActionResult Index(string? buscar, int pagina = 1)
    {
        const int porPagina = 10;
        pagina = Math.Max(1, pagina);
        var tipos = new List<TipoInmueble>();
        var total = 0;
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            using var totalCommand = new MySqlCommand(
                "SELECT COUNT(*) FROM TiposInmueble WHERE (@buscar = '' OR Descripcion LIKE CONCAT('%', @buscar, '%'))", connection);
            totalCommand.Parameters.AddWithValue("@buscar", buscar?.Trim() ?? "");
            total = Convert.ToInt32(totalCommand.ExecuteScalar());
            pagina = Math.Min(pagina, Math.Max(1, (int)Math.Ceiling(total / (double)porPagina)));
            string sql = @"SELECT Id, Descripcion FROM TiposInmueble
                           WHERE (@buscar = '' OR Descripcion LIKE CONCAT('%', @buscar, '%'))
                           ORDER BY Descripcion, Id LIMIT @limite OFFSET @offset";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@buscar", buscar?.Trim() ?? "");
            command.Parameters.AddWithValue("@limite", porPagina);
            command.Parameters.AddWithValue("@offset", (pagina - 1) * porPagina);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tipos.Add(new TipoInmueble
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Descripcion = reader["Descripcion"].ToString() ?? ""
                });
            }
        }
        return View(new PaginadoViewModel<TipoInmueble>
        {
            Elementos = tipos,
            Pagina = pagina,
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)porPagina)),
            ValoresRuta = new Dictionary<string, string?> { ["buscar"] = buscar }
        });
    }

    public IActionResult Details(int id)
    {
        TipoInmueble? tipo = null;
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, Descripcion FROM TiposInmueble WHERE Id = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                tipo = new TipoInmueble
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Descripcion = reader["Descripcion"].ToString() ?? ""
                };
            }
        }
        if (tipo == null) return NotFound();
        return View(tipo);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TipoInmueble tipo)
    {
        if (!ModelState.IsValid) return View(tipo);

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "INSERT INTO TiposInmueble (Descripcion) VALUES (@descripcion)";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@descripcion", tipo.Descripcion);
            command.ExecuteNonQuery();
        }
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var tipo = ObtenerPorId(id);
        return tipo == null ? NotFound() : View(tipo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, TipoInmueble tipo)
    {
        if (id != tipo.Id) return BadRequest();
        if (!ModelState.IsValid) return View(tipo);

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "UPDATE TiposInmueble SET Descripcion = @descripcion WHERE Id = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@descripcion", tipo.Descripcion);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Delete(int id)
    {
        var tipo = ObtenerPorId(id);
        return tipo == null ? NotFound() : View(tipo);
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
            string sql = "DELETE FROM TiposInmueble WHERE Id = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            try
            {
                if (command.ExecuteNonQuery() == 0) return NotFound();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                var tipo = ObtenerPorId(id);
                if (tipo == null) return NotFound();
                ModelState.AddModelError(string.Empty, "No se puede eliminar este tipo porque está asociado a uno o más inmuebles.");
                return View("Delete", tipo);
            }
        }
        return RedirectToAction(nameof(Index));
    }

    private TipoInmueble? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand("SELECT Id, Descripcion FROM TiposInmueble WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new TipoInmueble { Id = reader.GetInt32("Id"), Descripcion = reader.GetString("Descripcion") }
            : null;
    }
}