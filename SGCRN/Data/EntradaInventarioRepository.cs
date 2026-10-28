using System.Data;
using Microsoft.EntityFrameworkCore;
using SGCRN.Models;

namespace SGCRN.Data;

public sealed class EntradaInventarioRepository(SGCRNDbContext db)
{
    public async Task<IReadOnlyList<EntradaInventario>> GetAllAsync() =>
        await db.EntradasInventario.AsNoTracking()
            .Include(e => e.MateriaPrima).Include(e => e.ProductoTerminado)
            .OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id)
            .Take(100).ToListAsync();

    public Task<EntradaInventario?> GetByIdAsync(long id) =>
        db.EntradasInventario.AsNoTracking()
            .Include(e => e.MateriaPrima).Include(e => e.ProductoTerminado)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<long> RegistrarAsync(EntradaInventario entrada)
    {
        // El incremento condicional evita perder actualizaciones simultáneas. La entrada
        // y el stock se confirman juntos; ante cualquier error se deshacen ambos.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            const decimal stockMaximo = 999999999999999.999m;
            var maximoAnterior = stockMaximo - entrada.Cantidad;
            int modificados;
            if (entrada.TipoOrigen == TipoOrigenEntrada.Compra && entrada.MateriaPrimaId is long materiaId)
            {
                modificados = await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE MateriasPrimas SET StockActual = StockActual + {entrada.Cantidad}
                    WHERE Id = {materiaId} AND Activo = 1 AND StockActual <= {maximoAnterior}
                    """);
            }
            else if (entrada.TipoOrigen == TipoOrigenEntrada.Produccion && entrada.ProductoTerminadoId is long productoId)
            {
                modificados = await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE ProductosTerminados SET StockActual = StockActual + {entrada.Cantidad}
                    WHERE Id = {productoId} AND Activo = 1 AND StockActual <= {maximoAnterior}
                    """);
            }
            else
            {
                throw new InvalidOperationException("La entrada debe indicar exactamente un destino válido.");
            }

            if (modificados != 1)
                throw new InvalidOperationException("El destino ya no existe, está inactivo o excedería el límite de existencias.");

            db.EntradasInventario.Add(entrada);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return entrada.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
