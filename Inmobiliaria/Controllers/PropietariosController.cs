using Inmobiliaria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Inmobiliaria.Controllers;

[Authorize]
public class PropietariosController : Controller
{
    private readonly string _connectionString;

    public PropietariosController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public IActionResult Index(string? buscar, int pagina = 1)
    {
        const int porPagina = 10;
        pagina = Math.Max(1, pagina);
        var propietarios = new List<Propietario>();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string filtro = @"WHERE (@buscar = '' OR Nombre LIKE CONCAT('%', @buscar, '%')
                                   OR Apellido LIKE CONCAT('%', @buscar, '%')
                                   OR Dni LIKE CONCAT('%', @buscar, '%')
                                   OR Email LIKE CONCAT('%', @buscar, '%'))";
        using var totalCommand = new MySqlCommand($"SELECT COUNT(*) FROM Propietarios {filtro}", connection);
        totalCommand.Parameters.AddWithValue("@buscar", buscar?.Trim() ?? "");
        var total = Convert.ToInt32(totalCommand.ExecuteScalar());
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)porPagina));
        pagina = Math.Min(pagina, totalPaginas);
        using var command = new MySqlCommand(
            $@"SELECT Id, Nombre, Apellido, Dni, Telefono, Email FROM Propietarios {filtro}
               ORDER BY Apellido, Nombre, Id LIMIT @limite OFFSET @offset", connection);
        command.Parameters.AddWithValue("@buscar", buscar?.Trim() ?? "");
        command.Parameters.AddWithValue("@limite", porPagina);
        command.Parameters.AddWithValue("@offset", (pagina - 1) * porPagina);
        using var reader = command.ExecuteReader();
        while (reader.Read()) propietarios.Add(Mapear(reader));
        return View(new PaginadoViewModel<Propietario>
        {
            Elementos = propietarios,
            Pagina = pagina,
            TotalPaginas = totalPaginas,
            ValoresRuta = new Dictionary<string, string?> { ["buscar"] = buscar }
        });
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Propietario propietario)
    {
        if (!ModelState.IsValid) return View(propietario);
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = "INSERT INTO Propietarios (Nombre, Apellido, Dni, Telefono, Email) VALUES (@nombre, @apellido, @dni, @telefono, @email)";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@nombre", propietario.Nombre);
        command.Parameters.AddWithValue("@apellido", propietario.Apellido);
        command.Parameters.AddWithValue("@dni", propietario.Dni);
        command.Parameters.AddWithValue("@telefono", propietario.Telefono);
        command.Parameters.AddWithValue("@email", propietario.Email);
        try { command.ExecuteNonQuery(); }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            ModelState.AddModelError(string.Empty, "Ya existe un propietario con esos datos.");
            return View(propietario);
        }
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var propietario = ObtenerPorId(id);
        return propietario == null ? NotFound() : View(propietario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Propietario propietario)
    {
        if (id != propietario.Id) return BadRequest();
        if (!ModelState.IsValid) return View(propietario);
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = "UPDATE Propietarios SET Nombre = @nombre, Apellido = @apellido, Dni = @dni, Telefono = @telefono, Email = @email WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@nombre", propietario.Nombre);
        command.Parameters.AddWithValue("@apellido", propietario.Apellido);
        command.Parameters.AddWithValue("@dni", propietario.Dni);
        command.Parameters.AddWithValue("@telefono", propietario.Telefono);
        command.Parameters.AddWithValue("@email", propietario.Email);
        command.ExecuteNonQuery();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Delete(int id)
    {
        var propietario = ObtenerPorId(id);
        return propietario == null ? NotFound() : View(propietario);
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand("DELETE FROM Propietarios WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
        return RedirectToAction(nameof(Index));
    }

    private Propietario? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand("SELECT Id, Nombre, Apellido, Dni, Telefono, Email FROM Propietarios WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    private static Propietario Mapear(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32("Id"),
        Nombre = reader.GetString("Nombre"),
        Apellido = reader.GetString("Apellido"),
        Dni = reader.GetString("Dni"),
        Telefono = reader.GetString("Telefono"),
        Email = reader.GetString("Email")
    };
}
