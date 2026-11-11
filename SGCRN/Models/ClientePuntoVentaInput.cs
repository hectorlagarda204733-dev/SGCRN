using System.ComponentModel.DataAnnotations;

namespace SGCRN.Models;

public class ClientePuntoVentaInput
{
    [Required(ErrorMessage = "El nombre del cliente o punto de venta es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre admite hasta 150 caracteres.")]
    public string NombreTienda { get; set; } = "";

    [StringLength(150, ErrorMessage = "El contacto admite hasta 150 caracteres.")]
    public string? Contacto { get; set; }

    [StringLength(300, ErrorMessage = "La dirección admite hasta 300 caracteres.")]
    public string? Direccion { get; set; }
}
