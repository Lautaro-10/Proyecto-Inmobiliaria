using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class EditarUsuarioViewModel
{
    public int Id { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, RegularExpression("Administrador|Empleado")]
    public string Rol { get; set; } = "Empleado";

    public bool Activo { get; set; }

    [MinLength(8), DataType(DataType.Password)]
    public string? NuevaPassword { get; set; }
}
