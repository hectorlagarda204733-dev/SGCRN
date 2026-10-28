using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SGCRN.Models;

namespace SGCRN.Data;

public sealed class MateriaPrimaRepository(SGCRNDbContext db)
{
    public async Task<IReadOnlyList<MateriaPrima>> GetAllAsync(string? buscar = null, bool? activo = null)
    {
        var query = db.MateriasPrimas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(m => m.Nombre.Contains(buscar.Trim()));
        if (activo.HasValue)
            query = query.Where(m => m.Activo == activo.Value);
        return await query.OrderBy(m => m.Nombre).ToListAsync();
    }

    public Task<MateriaPrima?> GetByIdAsync(long id) =>
        db.MateriasPrimas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    public async Task<long> CreateAsync(MateriaPrima materia)
    {
        try
        {
            db.MateriasPrimas.Add(materia);
            await db.SaveChangesAsync();
            return materia.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new MateriaPrimaDuplicadaException(ex);
        }
    }

    public async Task<bool> UpdateAsync(MateriaPrima materia)
    {
        var existente = await db.MateriasPrimas.FindAsync(materia.Id);
        if (existente is null) return false;

        // El catálogo no modifica stock ni el estado lógico de la materia prima.
        existente.Nombre = materia.Nombre;
        existente.Descripcion = materia.Descripcion;
        existente.UnidadMedida = materia.UnidadMedida;
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new MateriaPrimaDuplicadaException(ex);
        }
    }

    public async Task<bool> DeactivateAsync(long id)
    {
        var materia = await db.MateriasPrimas.FindAsync(id);
        if (materia is null || !materia.Activo) return false;
        materia.Activo = false;
        await db.SaveChangesAsync();
        return true;
    }
}
