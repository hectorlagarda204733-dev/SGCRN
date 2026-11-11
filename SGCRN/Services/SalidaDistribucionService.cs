using System.ComponentModel.DataAnnotations;
using SGCRN.Data;
using SGCRN.Models;

namespace SGCRN.Services;

public sealed class SalidaDistribucionService(SalidaDistribucionRepository salidas, ProductoRepository productos, ClientePuntoVentaRepository clientes)
{
    public Task<IReadOnlyList<SalidaDistribucion>> GetAllAsync() => salidas.GetAllAsync();
    public Task<SalidaDistribucion?> GetByIdAsync(long id) => salidas.GetByIdAsync(id);
    public Task<IReadOnlyList<ProductoTerminado>> GetProductosAsync() => productos.GetAllAsync(activo: true);
    public Task<IReadOnlyList<ClientePuntoVenta>> GetClientesAsync() => clientes.GetAllAsync();

    public async Task<long> RegistrarAsync(SalidaDistribucionInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        var salida = new SalidaDistribucion
        {
            ProductoTerminadoId = input.ProductoTerminadoId!.Value,
            ClientePuntoVentaId = input.ClientePuntoVentaId!.Value,
            TipoSalida = input.TipoSalida!.Value, Cantidad = input.Cantidad,
            Observaciones = input.Observaciones?.Trim(), ReferenciaRegistro = Guid.Parse(input.ReferenciaRegistro).ToString("D")
        };
        var anterior = await salidas.GetByReferenciaAsync(salida.ReferenciaRegistro);
        if (anterior is not null) return SalidaDistribucionRepository.Reutilizar(anterior, salida);
        var producto = await productos.GetByIdAsync(salida.ProductoTerminadoId);
        if (producto is null || !producto.Activo) throw new InvalidOperationException("Selecciona un producto activo.");
        if (await clientes.GetByIdAsync(salida.ClientePuntoVentaId) is null)
            throw new InvalidOperationException("El cliente o punto de venta ya no existe.");
        salida.UnidadMedida = producto.UnidadMedida;
        return await salidas.RegistrarAsync(salida);
    }
}
