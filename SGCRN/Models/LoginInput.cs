using System.ComponentModel.DataAnnotations;

namespace SGCRN.Models;

public sealed class LoginInput
{
    [Required(ErrorMessage = "Escribe tu usuario.")]
    [StringLength(50, ErrorMessage = "El usuario admite hasta 50 caracteres.")]
    public string Usuario { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [StringLength(200, ErrorMessage = "La contraseña admite hasta 200 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
