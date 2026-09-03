using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class Inmueble
{
    public int Id { get; set; }

    [Required]
    public int PropietarioId { get; set; }
    public Propietario? Duenio { get; set; }
    
    [Required]
    public int TipoInmuebleId { get; set; }
    public TipoInmueble? Tipo { get; set; }


    [Required(ErrorMessage = "Se necesita agregar una descripcion del inmueble")]
    public string Descripcion { get; set; } = string.Empty;
////////////////////////////////////////
    [Required(ErrorMessage = "La dirección es requerida.")]
    public string Direccion { get; set; } = string.Empty;

    [Required]
    [Range(1, 50, ErrorMessage = "El cupo debe ser mayor a 0.")]
    public int Cupo { get; set; }


    [Required]
    [Range(1, 10000000)]
    public decimal PrecioPorDia { get; set; }

    [Required]
    public decimal PorcentajeReserva { get; set; } = 10.0m;

    public bool Disponible { get; set; } = true;


    [Required]
    public int TipoInmueble { get; set; }
    public TipoInmueble? tipo { get; set; }

}