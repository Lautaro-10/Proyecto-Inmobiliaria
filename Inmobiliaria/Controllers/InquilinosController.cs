using Inmobiliaria.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
namespace Inmobiliaria.Controllers;

public class InquilinosController : Controller
{
    private readonly String _connectionString;

    public InquilinosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }



public IActionResult Index()
    {
        var inquilinos = new List<Inquilino>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, Nombre, Apellido, Dni, Telefono, Email FROM Inquilinos ORDER BY Apellido, Nombre";

            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    inquilinos.Add(new Inquilino
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nombre = reader["Nombre"].ToString() ?? "",
                        Apellido = reader["Apellido"].ToString() ?? "",
                        Dni = reader["Dni"].ToString() ?? "",
                        Telefono = reader["Telefono"].ToString() ?? "",
                        Email = reader["Email"].ToString() ?? ""
                    });
                }
            }
        }

        return View(inquilinos);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    
    public IActionResult Create(Inquilino inquilino)
    {
        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            string checkSql = "SELECT COUNT(1) FROM Inquilinos WHERE Email = @email OR Dni = @dni";
            using (var checkCommand = new MySqlCommand(checkSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@email", inquilino.Email);
                checkCommand.Parameters.AddWithValue("@dni", inquilino.Dni);

                long count = Convert.ToInt64(checkCommand.ExecuteScalar());
                if (count > 0)
                {
                    ModelState.AddModelError("", "Ya existe un inquilino con ese DNI o Email.");
                    return View(inquilino);
                }
            }

            string sql = @"INSERT INTO Inquilinos (Nombre, Apellido, Dni, Telefono, Email) 
                           VALUES (@nombre, @apellido, @dni, @telefono, @email)";

            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@nombre", inquilino.Nombre);
                command.Parameters.AddWithValue("@apellido", inquilino.Apellido);
                command.Parameters.AddWithValue("@dni", inquilino.Dni);
                command.Parameters.AddWithValue("@telefono", inquilino.Telefono);
                command.Parameters.AddWithValue("@email", inquilino.Email);

                command.ExecuteNonQuery();
            }
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        Inquilino? inquilino = null;

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, Nombre, Apellido, Dni, Telefono, Email FROM Inquilinos WHERE Id = @id";

            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        inquilino = new Inquilino
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString() ?? "",
                            Apellido = reader["Apellido"].ToString() ?? "",
                            Dni = reader["Dni"].ToString() ?? "",
                            Telefono = reader["Telefono"].ToString() ?? "",
                            Email = reader["Email"].ToString() ?? ""
                        };
                    }
                }
            }
        }

        return View(inquilino);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Inquilino inquilino)
    {
        if (id != inquilino.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }


        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string checksql = "SELECT count(1) FROM Inquilinos WHERE (Email = @email OR Dni = @dni) AND Id != @id";

            using (var checkCommand = new MySqlCommand(checksql, connection))
            {
                checkCommand.Parameters.AddWithValue("@email", inquilino.Email);
                checkCommand.Parameters.AddWithValue("@dni", inquilino.Dni);
                checkCommand.Parameters.AddWithValue("@id", id);

                long count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0)
                {
                    ModelState.AddModelError(nameof(Inquilino.Email), "Ya existe otro inquilino con ese email.");
                    return View(inquilino);
                }
            }
            string sql = @"UPDATE Inquilinos 
                           SET Nombre = @nombre, Apellido = @apellido, Dni = @dni, Telefono = @telefono, Email = @email 
                           WHERE Id = @id";

            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@nombre", inquilino.Nombre);
                command.Parameters.AddWithValue("@apellido", inquilino.Apellido);
                command.Parameters.AddWithValue("@dni", inquilino.Dni);
                command.Parameters.AddWithValue("@telefono", inquilino.Telefono);
                command.Parameters.AddWithValue("@email", inquilino.Email);
                command.Parameters.AddWithValue("@id", id);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return NotFound();
                }
            }
        }
                return RedirectToAction(nameof(Index));
        }
        

    public IActionResult Delete(int id)
    {
        Inquilino? inquilino = null;

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, Nombre, Apellido, Dni, Telefono, Email FROM Inquilinos WHERE Id = @id";

            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        inquilino = new Inquilino
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString() ?? "",
                            Apellido = reader["Apellido"].ToString() ?? "",
                            Dni = reader["Dni"].ToString() ?? "",
                            Telefono = reader["Telefono"].ToString() ?? "",
                            Email = reader["Email"].ToString() ?? ""
                        };
                    }
                }
            }
        }
        if (inquilino == null)
        {
            return NotFound();
        }

        return View(inquilino);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    
    public IActionResult DeleteConfirmed(int id)
    {
        using (var connection =new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "DELETE FROM Inquilinos WHERE Id = @id";

            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
        }
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "DELETE FROM Inquilinos WHERE Id = @id";

            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
        }

        return RedirectToAction(nameof(Index));
    }
} 

