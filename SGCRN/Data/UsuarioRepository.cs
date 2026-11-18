using Microsoft.EntityFrameworkCore;
using SGCRN.Models;

namespace SGCRN.Data;

public sealed class UsuarioRepository(SGCRNDbContext db)
{
    public Task<Usuario?> GetByNombreAsync(string nombre) =>
        db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.NombreUsuario == nombre);

    public async Task CrearAdministradorInicialAsync(string passwordHash)
    {
        // No reemplazar credenciales existentes al reiniciar la aplicación.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT IGNORE INTO Usuarios (NombreUsuario, PasswordHash, Rol, Activo)
            VALUES ({"admin"}, {passwordHash}, {"Administrador"}, {true})
            """);
    }
}
