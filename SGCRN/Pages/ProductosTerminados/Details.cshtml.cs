using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Pages.ProductosTerminados;

public class DetailsModel(ProductoTerminadoService productoTerminadoService) : PageModel
{
    public ProductoTerminado Producto { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var producto = await productoTerminadoService.GetByIdAsync(id);
        if (producto is null) return NotFound();
        Producto = producto;
        return Page();
    }
}
