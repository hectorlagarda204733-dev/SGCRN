using System.ComponentModel.DataAnnotations;
using SGCRN.Data;
using SGCRN.Models;

namespace SGCRN.Services;

public sealed class ClientePuntoVentaService(ClientePuntoVentaRepository clientes)
{
    public Task<IReadOnlyList<ClientePuntoVenta>> GetAllAsync(string? buscar = null) => clientes.GetAllAsync(buscar?.Trim());
    public Task<ClientePuntoVenta?> GetByIdAsync(long id) => clientes.GetByIdAsync(id);

    public Task<long> CreateAsync(ClientePuntoVentaInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        return clientes.CreateAsync(new ClientePuntoVenta
        {
            NombreTienda = input.NombreTienda.Trim(), Contacto = input.Contacto?.Trim(), Direccion = input.Direccion?.Trim()
        });
    }
}
