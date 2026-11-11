namespace SGCRN.Models;

public class ClientePuntoVenta
{
    public long Id { get; set; }
    public string NombreTienda { get; set; } = "";
    public string? Contacto { get; set; }
    public string? Direccion { get; set; }
    public DateTime CreadoEn { get; set; }
}
