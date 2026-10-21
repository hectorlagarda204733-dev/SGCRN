using Microsoft.AspNetCore.Mvc;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Controllers;

[AutoValidateAntiforgeryToken]
public class ProductoController(ProductoTerminadoService productoService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? buscar, bool? activo)
    {
        var model = new ProductoCatalogoViewModel
        {
            Buscar = buscar,
            Activo = activo,
            Productos = await productoService.GetAllAsync(buscar, activo)
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Create() => View(new ProductoTerminadoInput());

    [HttpPost]
    public async Task<IActionResult> Create(ProductoTerminadoInput input)
    {
        if (!ModelState.IsValid) return View(input);

        try
        {
            await productoService.CreateAsync(input);
            TempData["Mensaje"] = "Producto registrado correctamente.";
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
        var producto = await productoService.GetByIdAsync(id);
        return producto is null ? NotFound() : View(producto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var producto = await productoService.GetByIdAsync(id);
        if (producto is null) return NotFound();

        return View(new ProductoTerminadoInput
        {
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            ImagenUrl = producto.ImagenUrl,
            UnidadMedida = producto.UnidadMedida,
            PrecioSugerido = producto.PrecioSugerido
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, ProductoTerminadoInput input)
    {
        if (await productoService.GetByIdAsync(id) is null) return NotFound();
        if (!ModelState.IsValid) return View(input);

        try
        {
            await productoService.UpdateAsync(id, input);
            TempData["Mensaje"] = "Producto actualizado correctamente.";
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
        var producto = await productoService.GetByIdAsync(id);
        return producto is null ? NotFound() : View(producto);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var producto = await productoService.GetByIdAsync(id);
        if (producto is null) return NotFound();

        try
        {
            await productoService.DeactivateAsync(id);
            TempData["Mensaje"] = "Producto dado de baja. El registro se conserva en la base de datos.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Delete", producto);
        }
    }
}
