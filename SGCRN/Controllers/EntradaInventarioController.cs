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
    public async Task<IActionResult> Create()
    {
        await LoadDestinosAsync();
        return View(new EntradaInventarioInput());
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
