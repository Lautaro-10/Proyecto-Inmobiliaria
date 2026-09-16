using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class Reserva
{
    public int Id { get; set; }

    [Required]
    public int InmuebleId { get; set; }
    public Inmueble? Inmueble { get; set; }

    [Required]
    public int InquilinoId { get; set; }
    public Inquilino? Inquilino { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime FechaInicio { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime FechaFin { get; set; }

    [Required]
    public decimal MontoDiario { get; set; }

    [Required]
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int? CreadoPorUsuarioId { get; set; }
    public int? TerminadoPorUsuarioId { get; set; }
    public DateTime? FechaTerminacion { get; set; }
    public DateTime? FechaFinOriginal { get; set; }
    public Usuario? CreadoPorUsuario { get; set; }
    public Usuario? TerminadoPorUsuario { get; set; }

}