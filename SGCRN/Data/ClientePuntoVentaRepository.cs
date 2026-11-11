using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SGCRN.Models;

namespace SGCRN.Data;

public sealed class ClientePuntoVentaRepository(SGCRNDbContext db)
{
    public async Task<IReadOnlyList<ClientePuntoVenta>> GetAllAsync(string? buscar = null) =>
        await db.Set<ClientePuntoVenta>().AsNoTracking()
            .Where(c => buscar == null || c.NombreTienda.Contains(buscar))
            .OrderBy(c => c.NombreTienda).ToListAsync();

    public Task<ClientePuntoVenta?> GetByIdAsync(long id) =>
        db.Set<ClientePuntoVenta>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<long> CreateAsync(ClientePuntoVenta cliente)
    {
        db.Add(cliente);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new InvalidOperationException("Ya existe un cliente o punto de venta con ese nombre.", ex);
        }
        return cliente.Id;
    }
}
