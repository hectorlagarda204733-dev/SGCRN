namespace SGCRN.Models;

public sealed class Usuario
{
    public long Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = "Administrador";
    public bool Activo { get; set; } = true;
}
