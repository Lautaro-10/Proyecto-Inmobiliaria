using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Inmobiliaria.Models;

public class PerfilUsuarioViewModel
{
    public int Id { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    public string? AvatarActual { get; set; }

    public IFormFile? Avatar { get; set; }

    [DataType(DataType.Password)]
    public string? PasswordActual { get; set; }

    [MinLength(8), DataType(DataType.Password)]
    public string? NuevaPassword { get; set; }

    [DataType(DataType.Password)]
    public string? ConfirmarPassword { get; set; }
}
