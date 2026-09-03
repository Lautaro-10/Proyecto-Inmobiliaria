using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Models;

public class TipoInmueble
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La descripción es requerida.")]
    public string Descripcion { get; set; } = string.Empty;
}