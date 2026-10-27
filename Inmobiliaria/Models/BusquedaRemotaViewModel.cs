namespace Inmobiliaria.Models;

public class BusquedaRemotaViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int? IdSeleccionado { get; set; }
    public string? TextoSeleccionado { get; set; }
    public string Placeholder { get; set; } = "Escriba para buscar";
}
