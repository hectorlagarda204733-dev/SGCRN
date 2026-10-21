using System.ComponentModel.DataAnnotations;

namespace SGCRN.Models;

public class ProductoTerminadoInput : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre admite hasta 150 caracteres.")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Descripción")]
    [StringLength(500, ErrorMessage = "La descripción admite hasta 500 caracteres.")]
    public string? Descripcion { get; set; }

    [Display(Name = "URL de imagen (opcional)")]
    [StringLength(500)]
    public string? ImagenUrl { get; set; }

    [Display(Name = "Unidad de medida")]
    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(30)]
    public string UnidadMedida { get; set; } = "";

    [Display(Name = "Precio sugerido (MXN)")]
    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "El precio debe estar entre 0 y 999999999999.")]
    public decimal PrecioSugerido { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Round(PrecioSugerido, 2) != PrecioSugerido)
            yield return new("El precio admite hasta dos decimales.", [nameof(PrecioSugerido)]);

        if (!string.IsNullOrWhiteSpace(ImagenUrl) &&
            (!Uri.TryCreate(ImagenUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            yield return new("Usa una URL de imagen que comience con http:// o https://.", [nameof(ImagenUrl)]);
    }
}
