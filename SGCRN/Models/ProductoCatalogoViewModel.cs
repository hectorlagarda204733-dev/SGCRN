namespace SGCRN.Models;

public class ProductoCatalogoViewModel
{
    public string? Buscar { get; set; }
    public bool? Activo { get; set; }
    public IReadOnlyList<ProductoTerminado> Productos { get; set; } = [];
}
