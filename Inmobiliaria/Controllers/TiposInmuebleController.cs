using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Inmobiliaria.Controllers;

public class TiposInmuebleController : Controller
{
    private readonly string _connectionString;

    public TiposInmuebleController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    public IActionResult Index()
    {
        var tipos = new List<TipoInmueble>();
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, Descripcion FROM TiposInmueble ORDER BY Descripcion";
            using var command = new MySqlCommand(sql, connection);
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
        return View(tipos);
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

    public IActionResult Edit(int id) => Details(id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, TipoInmueble tipo)
    {
        if (id != tipo.Id || !ModelState.IsValid) return View(tipo);

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

    public IActionResult Delete(int id) => Details(id);

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "DELETE FROM TiposInmueble WHERE Id = @id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }
        return RedirectToAction(nameof(Index));
    }
}