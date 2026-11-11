namespace SGCRN.Models;

public enum TipoSalida { VentaDirecta, Distribucion }

public class SalidaDistribucion
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public TipoSalida TipoSalida { get; set; }
    public long ProductoTerminadoId { get; set; }
    public ProductoTerminado ProductoTerminado { get; set; } = null!;
    public long ClientePuntoVentaId { get; set; }
    public ClientePuntoVenta ClientePuntoVenta { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = "";
    public string? Observaciones { get; set; }
    public string ReferenciaRegistro { get; set; } = "";
}
