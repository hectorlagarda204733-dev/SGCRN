namespace SGCRN.Models;

public class EntradaInventario
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public TipoOrigenEntrada TipoOrigen { get; set; }
    public long? MateriaPrimaId { get; set; }
    public MateriaPrima? MateriaPrima { get; set; }
    public long? ProductoTerminadoId { get; set; }
    public ProductoTerminado? ProductoTerminado { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? Observaciones { get; set; }
}
