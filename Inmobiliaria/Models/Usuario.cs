using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [StringLength(255)]
    public string? AvatarUrl { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Rol { get; set; } = "Empleado";

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
}
