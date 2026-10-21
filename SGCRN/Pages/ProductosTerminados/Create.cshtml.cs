using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Pages.ProductosTerminados;

public class CreateModel(ProductoTerminadoService productoTerminadoService) : PageModel
{
    [BindProperty]
    public ProductoTerminadoInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            await productoTerminadoService.CreateAsync(Input);
            TempData["Mensaje"] = "Producto registrado correctamente.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Input.Nombre", ex.Message);
            return Page();
        }
    }
}
