using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Pages.ProductosTerminados;

public class DeleteModel(ProductoTerminadoService productoTerminadoService) : PageModel
{
    public ProductoTerminado Producto { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var producto = await productoTerminadoService.GetByIdAsync(id);
        if (producto is null) return NotFound();
        Producto = producto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id)
    {
        var producto = await productoTerminadoService.GetByIdAsync(id);
        if (producto is null) return NotFound();
        Producto = producto;

        try
        {
            await productoTerminadoService.DeactivateAsync(id);
            TempData["Mensaje"] = "Producto dado de baja. El registro se conserva en la base de datos.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
