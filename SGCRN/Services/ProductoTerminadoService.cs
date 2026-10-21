using System.ComponentModel.DataAnnotations;
using SGCRN.Data;
using SGCRN.Models;

namespace SGCRN.Services;

public sealed class ProductoTerminadoService(ProductoRepository productoRepository)
{
    public Task<IReadOnlyList<ProductoTerminado>> GetAllAsync(string? buscar = null, bool? activo = null) =>
        productoRepository.GetAllAsync(buscar?.Trim(), activo);

    public Task<ProductoTerminado?> GetByIdAsync(long id) => productoRepository.GetByIdAsync(id);

    public async Task<long> CreateAsync(ProductoTerminadoInput input)
    {
        var producto = BuildProduct(input);
        producto.Activo = true;

        try
        {
            return await productoRepository.CreateAsync(producto);
        }
        catch (ProductoDuplicadoException)
        {
            throw DuplicateNameException();
        }
    }

    public async Task UpdateAsync(long id, ProductoTerminadoInput input)
    {
        var producto = BuildProduct(input);
        producto.Id = id;

        try
        {
            // La edición de información conserva el estado del producto.
            if (!await productoRepository.UpdateAsync(producto))
                throw new InvalidOperationException("El producto ya no existe.");
        }
        catch (ProductoDuplicadoException)
        {
            throw DuplicateNameException();
        }
    }

    public async Task DeactivateAsync(long id)
    {
        if (!await productoRepository.DeactivateAsync(id))
            throw new InvalidOperationException("El producto ya está inactivo o no existe.");
    }

    private static ProductoTerminado BuildProduct(ProductoTerminadoInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), validateAllProperties: true);
        return new ProductoTerminado
        {
            Nombre = input.Nombre.Trim(),
            Descripcion = input.Descripcion?.Trim(),
            ImagenUrl = input.ImagenUrl?.Trim(),
            UnidadMedida = input.UnidadMedida.Trim(),
            PrecioSugerido = input.PrecioSugerido
        };
    }

    private static InvalidOperationException DuplicateNameException() =>
        new("Ya existe un producto con ese nombre, incluso si está inactivo.");
}
