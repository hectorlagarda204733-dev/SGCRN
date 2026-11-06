using Microsoft.AspNetCore.Mvc;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Controllers;

[AutoValidateAntiforgeryToken]
public class EntradaInventarioController(InventarioService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await service.GetAllAsync());

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var entrada = await service.GetByIdAsync(id);
        return entrada is null ? NotFound() : View(entrada);
    }

    [HttpGet]
    public async Task<IActionResult> Create(long? productoId, long? materiaId)
    {
        await LoadDestinosAsync();
        var input = new EntradaInventarioInput();
        if (productoId is not null && materiaId is null && ((IReadOnlyList<ProductoTerminado>)ViewData["Productos"]!).Any(x => x.Id == productoId))
        {
            input.TipoOrigen = TipoOrigenEntrada.Produccion;
            input.ProductoTerminadoId = productoId;
        }
        else if (materiaId is not null && productoId is null && ((IReadOnlyList<MateriaPrima>)ViewData["MateriasPrimas"]!).Any(x => x.Id == materiaId))
        {
            input.TipoOrigen = TipoOrigenEntrada.Compra;
            input.MateriaPrimaId = materiaId;
        }
        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> Create(EntradaInventarioInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await service.RegistrarAsync(input);
                TempData["Mensaje"] = "Entrada registrada y existencias actualizadas correctamente.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await LoadDestinosAsync();
        return View(input);
    }

    private async Task LoadDestinosAsync()
    {
        var (materias, productos) = await service.GetDestinosAsync();
        ViewData["MateriasPrimas"] = materias;
        ViewData["Productos"] = productos;
    }
}
