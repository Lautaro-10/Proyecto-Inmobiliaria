using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class ReservaListadoViewModel
{
    public List<Reserva> Reservas { get; set; } = [];
    public string? Buscar { get; set; }
    public string? Estado { get; set; }
    public int Pagina { get; set; }
    public int TotalPaginas { get; set; }
}

public class DisponibilidadViewModel
{
    [Required, DataType(DataType.Date)]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(1);

    public int? TipoInmuebleId { get; set; }
    public int? CupoMinimo { get; set; }
    public List<Inmueble> Inmuebles { get; set; } = [];
}

public class TerminarReservaViewModel
{
    public int ReservaId { get; set; }
    public DateTime FechaTerminacion { get; set; }
    public decimal Multa { get; set; }
    public decimal DiasRestantes { get; set; }
    public decimal MontoDiario { get; set; }
    public string Concepto { get; set; } = "Multa por finalización anticipada";
    public bool ConfirmarPago { get; set; }
    public Reserva? Reserva { get; set; }
}

public class RenovarReservaViewModel
{
    public int ReservaId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal MontoDiario { get; set; }
    public Reserva? Reserva { get; set; }
}

public class InformeInmuebleViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public int CantidadReservas { get; set; }
    public DateTime? UltimaReserva { get; set; }
}

public class InformeReservaViewModel
{
    public int Id { get; set; }
    public string Inmueble { get; set; } = string.Empty;
    public string Inquilino { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
}
