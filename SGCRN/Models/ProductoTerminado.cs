namespace SGCRN.Models;

public class ProductoTerminado
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public string UnidadMedida { get; set; } = "";
    public decimal PrecioSugerido { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}
