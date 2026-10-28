using System.ComponentModel.DataAnnotations;

namespace SGCRN.Models;

public class EntradaInventarioInput : IValidatableObject
{
    [Display(Name = "Origen de la entrada")]
    [Required(ErrorMessage = "Selecciona el origen de la entrada.")]
    public TipoOrigenEntrada? TipoOrigen { get; set; }

    [Display(Name = "Materia prima recibida")]
    public long? MateriaPrimaId { get; set; }

    [Display(Name = "Producto terminado producido")]
    public long? ProductoTerminadoId { get; set; }

    [Display(Name = "Cantidad recibida")]
    [Range(typeof(decimal), "0.001", "999999999999.999", ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public decimal Cantidad { get; set; }

    [Display(Name = "Costo unitario (MXN)")]
    [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "El costo no puede ser negativo.")]
    public decimal CostoUnitario { get; set; }

    [Display(Name = "Observaciones")]
    [StringLength(500, ErrorMessage = "Las observaciones admiten hasta 500 caracteres.")]
    public string? Observaciones { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TipoOrigen is { } tipo && !Enum.IsDefined(tipo))
            yield return new("El origen seleccionado no es válido.", [nameof(TipoOrigen)]);

        if (Cantidad > 0 && decimal.Round(Cantidad, 3) != Cantidad)
            yield return new("La cantidad admite hasta tres decimales.", [nameof(Cantidad)]);

        if (CostoUnitario >= 0 && decimal.Round(CostoUnitario, 2) != CostoUnitario)
            yield return new("El costo admite hasta dos decimales.", [nameof(CostoUnitario)]);

        if (MateriaPrimaId is <= 0)
            yield return new("Selecciona una materia prima válida.", [nameof(MateriaPrimaId)]);

        if (ProductoTerminadoId is <= 0)
            yield return new("Selecciona un producto válido.", [nameof(ProductoTerminadoId)]);

        if (TipoOrigen == TipoOrigenEntrada.Compra && (MateriaPrimaId is null || ProductoTerminadoId is not null))
            yield return new("La compra debe indicar solo una materia prima.", [nameof(MateriaPrimaId)]);

        if (TipoOrigen == TipoOrigenEntrada.Produccion && (ProductoTerminadoId is null || MateriaPrimaId is not null))
            yield return new("La producción debe indicar solo un producto terminado.", [nameof(ProductoTerminadoId)]);
    }
}
