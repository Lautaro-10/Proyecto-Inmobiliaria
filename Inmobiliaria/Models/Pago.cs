using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class Pago
{
    public int Id { get; set; }
    [Required] public int ReservaId { get; set; }
    public Reserva? Reserva { get; set; }
    [Required] public decimal Monto { get; set; }
    [Required, StringLength(100)] public string Concepto { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Estado { get; set; } = "Pendiente";
    public DateTime? FechaPago { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int? CreadoPorUsuarioId { get; set; }
    public int? ModificadoPorUsuarioId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Usuario? CreadoPorUsuario { get; set; }
    public Usuario? ModificadoPorUsuario { get; set; }
}
