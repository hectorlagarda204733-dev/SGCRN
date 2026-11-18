using Microsoft.AspNetCore.Identity;
using SGCRN.Data;
using SGCRN.Models;

namespace SGCRN.Services;

public sealed class AutenticacionService(UsuarioRepository usuarios, IPasswordHasher<Usuario> hasher)
{
    // Usado también para que un usuario inexistente ejecute la verificación del hash.
    private static readonly Usuario Inicial = new() { NombreUsuario = "admin" };
    private static readonly string HashInicial = new PasswordHasher<Usuario>().HashPassword(Inicial, "admin");

    public Task PrepararAdministradorAsync() => usuarios.CrearAdministradorInicialAsync(HashInicial);

    public async Task<Usuario?> ValidarAsync(string nombre, string password)
    {
        var usuario = await usuarios.GetByNombreAsync(nombre.Trim());
        var resultado = hasher.VerifyHashedPassword(usuario ?? Inicial, usuario?.PasswordHash ?? HashInicial, password);
        return usuario is { Activo: true, Rol: "Administrador" } && resultado != PasswordVerificationResult.Failed
            ? usuario : null;
    }
}
