using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Pages.ProductosTerminados;

public class EditModel(ProductoTerminadoService productoTerminadoService) : PageModel
{
    [BindProperty]
    public ProductoTerminadoInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var producto = await productoTerminadoService.GetByIdAsync(id);
        if (producto is null) return NotFound();

        Input = new ProductoTerminadoInput
        {
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            ImagenUrl = producto.ImagenUrl,
            UnidadMedida = producto.UnidadMedida,
            PrecioSugerido = producto.PrecioSugerido
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id)
    {
        if (await productoTerminadoService.GetByIdAsync(id) is null) return NotFound();
        if (!ModelState.IsValid) return Page();

        try
        {
            await productoTerminadoService.UpdateAsync(id, Input);
            TempData["Mensaje"] = "Producto actualizado correctamente.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Input.Nombre", ex.Message);
            return Page();
        }
    }
}
