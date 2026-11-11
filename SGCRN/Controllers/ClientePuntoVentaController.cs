using Microsoft.AspNetCore.Mvc;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Controllers;

[AutoValidateAntiforgeryToken]
public class ClientePuntoVentaController(ClientePuntoVentaService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? buscar)
    {
        ViewData["Buscar"] = buscar;
        return View(await service.GetAllAsync(buscar));
    }

    [HttpGet]
    public IActionResult Create(long? productoId)
    {
        ViewData["ProductoId"] = productoId;
        return View(new ClientePuntoVentaInput());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ClientePuntoVentaInput input, long? productoId)
    {
        ViewData["ProductoId"] = productoId;
        if (!ModelState.IsValid) return View(input);
        try
        {
            var id = await service.CreateAsync(input);
            TempData["Mensaje"] = "Cliente o punto de venta registrado correctamente.";
            return RedirectToAction("Create", "SalidaDistribucion", new { clienteId = id, productoId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(input.NombreTienda), ex.Message);
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var cliente = await service.GetByIdAsync(id);
        return cliente is null ? NotFound() : View(cliente);
    }
}
