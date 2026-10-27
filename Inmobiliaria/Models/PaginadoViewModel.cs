namespace Inmobiliaria.Models;

public class PaginacionViewModel
{
    public int Pagina { get; set; }
    public int TotalPaginas { get; set; }
    public Dictionary<string, string?> ValoresRuta { get; set; } = [];
}

public class PaginadoViewModel<T> : PaginacionViewModel
{
    public List<T> Elementos { get; set; } = [];
}
