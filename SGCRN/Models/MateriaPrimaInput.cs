using System.ComponentModel.DataAnnotations;

namespace SGCRN.Models;

public class MateriaPrimaInput : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre admite hasta 150 caracteres.")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Descripción")]
    [StringLength(500, ErrorMessage = "La descripción admite hasta 500 caracteres.")]
    public string? Descripcion { get; set; }

    [Display(Name = "Unidad de medida")]
    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(30, ErrorMessage = "La unidad admite hasta 30 caracteres.")]
    public string UnidadMedida { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(UnidadMedida) && !CatalogoUnidadesMedida.Contiene(UnidadMedida))
            yield return new ValidationResult("Selecciona una unidad de medida del catálogo.", new[] { nameof(UnidadMedida) });
    }
}
