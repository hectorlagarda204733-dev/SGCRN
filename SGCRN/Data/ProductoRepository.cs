using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SGCRN.Models;

namespace SGCRN.Data;

public sealed class ProductoRepository(SGCRNDbContext db)
{
    public async Task<IReadOnlyList<ProductoTerminado>> GetAllAsync(string? buscar = null, bool? activo = null)
    {
        var query = db.ProductosTerminados.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(p => p.Nombre.Contains(buscar.Trim()));
        if (activo.HasValue)
            query = query.Where(p => p.Activo == activo.Value);
        return await query.OrderBy(p => p.Nombre).ToListAsync();
    }

    public Task<ProductoTerminado?> GetByIdAsync(long id) =>
        db.ProductosTerminados.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<long> CreateAsync(ProductoTerminado producto)
    {
        try
        {
            db.ProductosTerminados.Add(producto);
            await db.SaveChangesAsync();
            return producto.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new ProductoDuplicadoException(ex);
        }
    }

    public async Task<bool> UpdateAsync(ProductoTerminado producto)
    {
        var existente = await db.ProductosTerminados.FindAsync(producto.Id);
        if (existente is null) return false;

        // La edición del catálogo no modifica las existencias ni la activación.
        existente.Nombre = producto.Nombre;
        existente.Descripcion = producto.Descripcion;
        existente.ImagenUrl = producto.ImagenUrl;
        existente.UnidadMedida = producto.UnidadMedida;
        existente.PrecioSugerido = producto.PrecioSugerido;
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new ProductoDuplicadoException(ex);
        }
    }

    public async Task<bool> DeactivateAsync(long id)
    {
        var producto = await db.ProductosTerminados.FindAsync(id);
        if (producto is null || !producto.Activo) return false;
        producto.Activo = false;
        await db.SaveChangesAsync();
        return true;
    }
}
