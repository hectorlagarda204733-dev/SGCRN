using Microsoft.AspNetCore.Mvc;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Controllers;

[AutoValidateAntiforgeryToken]
public class SalidaDistribucionController(SalidaDistribucionService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await service.GetAllAsync());

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var salida = await service.GetByIdAsync(id);
        return salida is null ? NotFound() : View(salida);
    }

    [HttpGet]
    public async Task<IActionResult> Create(long? productoId, long? clienteId)
    {
        await LoadOpcionesAsync();
        return View(new SalidaDistribucionInput
        {
            ReferenciaRegistro = Guid.NewGuid().ToString("D"),
            ProductoTerminadoId = ((IReadOnlyList<ProductoTerminado>)ViewData["Productos"]!).Any(p => p.Id == productoId) ? productoId : null,
            ClientePuntoVentaId = ((IReadOnlyList<ClientePuntoVenta>)ViewData["Clientes"]!).Any(c => c.Id == clienteId) ? clienteId : null
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(SalidaDistribucionInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await service.RegistrarAsync(input);
                TempData["Mensaje"] = "Salida registrada y existencias descontadas correctamente.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        await LoadOpcionesAsync();
        return View(input);
    }

    private async Task LoadOpcionesAsync()
    {
        ViewData["Productos"] = await service.GetProductosAsync();
        ViewData["Clientes"] = await service.GetClientesAsync();
    }
}
