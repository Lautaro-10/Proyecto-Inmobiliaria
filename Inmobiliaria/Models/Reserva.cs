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
    public bool estado { get;set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime fecha_creacion { get; set; }



}