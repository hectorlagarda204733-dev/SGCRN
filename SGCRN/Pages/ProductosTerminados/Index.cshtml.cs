using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Pages.ProductosTerminados;

public class IndexModel(ProductoTerminadoService productoTerminadoService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? Activo { get; set; }

    public IReadOnlyList<ProductoTerminado> Productos { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Productos = await productoTerminadoService.GetAllAsync(Buscar, Activo);
    }
}
