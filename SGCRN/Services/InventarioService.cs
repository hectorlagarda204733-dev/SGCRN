using System.ComponentModel.DataAnnotations;
using SGCRN.Data;
using SGCRN.Models;

namespace SGCRN.Services;

public sealed class InventarioService(
    EntradaInventarioRepository entradas,
    MateriaPrimaRepository materias,
    ProductoRepository productos)
{
    public Task<IReadOnlyList<EntradaInventario>> GetAllAsync() => entradas.GetAllAsync();
    public Task<EntradaInventario?> GetByIdAsync(long id) => entradas.GetByIdAsync(id);

    public async Task<(IReadOnlyList<MateriaPrima> Materias, IReadOnlyList<ProductoTerminado> Productos)> GetDestinosAsync()
    {
        // Comparten el mismo DbContext por solicitud: las consultas se hacen secuencialmente.
        var listaMaterias = await materias.GetAllAsync(activo: true);
        var listaProductos = await productos.GetAllAsync(activo: true);
        return (listaMaterias, listaProductos);
    }

    public async Task<long> RegistrarAsync(EntradaInventarioInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), validateAllProperties: true);
        if (input.TipoOrigen == TipoOrigenEntrada.Compra)
        {
            var materia = await materias.GetByIdAsync(input.MateriaPrimaId!.Value);
            if (materia is null || !materia.Activo)
                throw new InvalidOperationException("La materia prima no existe o está inactiva.");
        }
        else if (input.TipoOrigen == TipoOrigenEntrada.Produccion)
        {
            var producto = await productos.GetByIdAsync(input.ProductoTerminadoId!.Value);
            if (producto is null || !producto.Activo)
                throw new InvalidOperationException("El producto no existe o está inactivo.");
        }
        else
        {
            throw new InvalidOperationException("El origen de la entrada no es válido.");
        }

        return await entradas.RegistrarAsync(new EntradaInventario
        {
            TipoOrigen = input.TipoOrigen.Value,
            MateriaPrimaId = input.MateriaPrimaId,
            ProductoTerminadoId = input.ProductoTerminadoId,
            Cantidad = input.Cantidad,
            CostoUnitario = input.CostoUnitario,
            Observaciones = input.Observaciones?.Trim()
        });
    }
}
