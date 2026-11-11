using System.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SGCRN.Models;

namespace SGCRN.Data;

public sealed class SalidaDistribucionRepository(SGCRNDbContext db)
{
    public async Task<IReadOnlyList<SalidaDistribucion>> GetAllAsync() => await db.SalidasDistribucion.AsNoTracking()
        .Include(s => s.ProductoTerminado).Include(s => s.ClientePuntoVenta)
        .OrderByDescending(s => s.Fecha).ThenByDescending(s => s.Id).Take(100).ToListAsync();

    public Task<SalidaDistribucion?> GetByIdAsync(long id) => db.SalidasDistribucion.AsNoTracking()
        .Include(s => s.ProductoTerminado).Include(s => s.ClientePuntoVenta).FirstOrDefaultAsync(s => s.Id == id);

    public Task<SalidaDistribucion?> GetByReferenciaAsync(string referencia) =>
        db.SalidasDistribucion.AsNoTracking().FirstOrDefaultAsync(s => s.ReferenciaRegistro == referencia);

    public static long Reutilizar(SalidaDistribucion anterior, SalidaDistribucion solicitada)
    {
        if (anterior.ProductoTerminadoId != solicitada.ProductoTerminadoId || anterior.ClientePuntoVentaId != solicitada.ClientePuntoVentaId
            || anterior.Cantidad != solicitada.Cantidad || anterior.TipoSalida != solicitada.TipoSalida || anterior.Observaciones != solicitada.Observaciones)
            throw new InvalidOperationException("Este formulario ya registró una salida distinta. Abre una nueva salida.");
        return anterior.Id;
    }

    public async Task<long> RegistrarAsync(SalidaDistribucion salida)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // Bloquear/descontar el producto antes del INSERT evita conversiones de
            // bloqueos compartidos de la FK entre salidas concurrentes.
            var modificados = await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ProductosTerminados SET StockActual = StockActual - {salida.Cantidad}
                WHERE Id = {salida.ProductoTerminadoId} AND Activo = 1 AND StockActual >= {salida.Cantidad}
                """);
            if (modificados != 1)
                throw new InvalidOperationException("No hay existencias suficientes o el producto ya no está activo. Revisa la cantidad disponible.");
            db.SalidasDistribucion.Add(salida);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return salida.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            await transaction.RollbackAsync();
            db.Entry(salida).State = EntityState.Detached;
            var anterior = await GetByReferenciaAsync(salida.ReferenciaRegistro);
            if (anterior is null) throw;
            return Reutilizar(anterior, salida);
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
            // Un reenvío simultáneo puede llegar después de que el original agote el stock.
            var anterior = await GetByReferenciaAsync(salida.ReferenciaRegistro);
            if (anterior is not null) return Reutilizar(anterior, salida);
            throw;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
