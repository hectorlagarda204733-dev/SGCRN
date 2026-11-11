using System.ComponentModel.DataAnnotations;

namespace SGCRN.Models;

public class SalidaDistribucionInput : IValidatableObject
{
    [Required(ErrorMessage = "Selecciona el tipo de salida.")]
    public TipoSalida? TipoSalida { get; set; }

    [Required(ErrorMessage = "Selecciona un producto.")]
    [Range(1, long.MaxValue, ErrorMessage = "Selecciona un producto válido.")]
    public long? ProductoTerminadoId { get; set; }

    [Required(ErrorMessage = "Selecciona un cliente o punto de venta.")]
    [Range(1, long.MaxValue, ErrorMessage = "Selecciona un destinatario válido.")]
    public long? ClientePuntoVentaId { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999.999", ErrorMessage = "La cantidad debe ser mayor que cero y estar dentro del límite permitido.")]
    public decimal Cantidad { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones admiten hasta 500 caracteres.")]
    public string? Observaciones { get; set; }

    [Required(ErrorMessage = "Abre un nuevo formulario para registrar la salida.")]
    [StringLength(36)]
    public string ReferenciaRegistro { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TipoSalida is { } tipo && !Enum.IsDefined(tipo))
            yield return new("Selecciona un tipo de salida válido.", [nameof(TipoSalida)]);
        if (decimal.Round(Cantidad, 3) != Cantidad)
            yield return new("La cantidad admite hasta tres decimales.", [nameof(Cantidad)]);
        if (!Guid.TryParseExact(ReferenciaRegistro, "D", out var referencia) || referencia == Guid.Empty)
            yield return new("Abre un nuevo formulario para registrar la salida.", [nameof(ReferenciaRegistro)]);
    }
}
