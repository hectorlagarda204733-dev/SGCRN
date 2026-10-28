using Microsoft.AspNetCore.Mvc;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Controllers;

[AutoValidateAntiforgeryToken]
public class MateriaPrimaController(MateriaPrimaService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? buscar, bool? activo) => View(new MateriaPrimaCatalogoViewModel
    {
        Buscar = buscar,
        Activo = activo,
        MateriasPrimas = await service.GetAllAsync(buscar, activo)
    });

    [HttpGet]
    public IActionResult Create() => View(new MateriaPrimaInput());

    [HttpPost]
    public async Task<IActionResult> Create(MateriaPrimaInput input)
    {
        if (!ModelState.IsValid) return View(input);
        try
        {
            await service.CreateAsync(input);
            TempData["Mensaje"] = "Materia prima registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(input.Nombre), ex.Message);
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var materia = await service.GetByIdAsync(id);
        return materia is null ? NotFound() : View(materia);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var materia = await service.GetByIdAsync(id);
        return materia is null ? NotFound() : View(new MateriaPrimaInput
        {
            Nombre = materia.Nombre,
            Descripcion = materia.Descripcion,
            UnidadMedida = materia.UnidadMedida
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, MateriaPrimaInput input)
    {
        if (await service.GetByIdAsync(id) is null) return NotFound();
        if (!ModelState.IsValid) return View(input);
        try
        {
            await service.UpdateAsync(id, input);
            TempData["Mensaje"] = "Materia prima actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(input.Nombre), ex.Message);
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(long id)
    {
        var materia = await service.GetByIdAsync(id);
        return materia is null ? NotFound() : View(materia);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var materia = await service.GetByIdAsync(id);
        if (materia is null) return NotFound();
        try
        {
            await service.DeactivateAsync(id);
            TempData["Mensaje"] = "Materia prima dada de baja; el registro y sus entradas se conservan.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Delete", materia);
        }
    }
}
