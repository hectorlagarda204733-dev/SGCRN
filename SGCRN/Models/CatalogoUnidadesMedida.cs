namespace SGCRN.Models;

public static class CatalogoUnidadesMedida
{
    public sealed record Unidad(string Valor, string Nombre, string Grupo);

    public static IReadOnlyList<Unidad> Todas { get; } = Array.AsReadOnly(new[]
    {
        new Unidad("mg", "Miligramo (mg)", "Peso"),
        new Unidad("g", "Gramo (g)", "Peso"),
        new Unidad("kg", "Kilogramo (kg)", "Peso"),
        new Unidad("ml", "Mililitro (ml)", "Volumen"),
        new Unidad("litro", "Litro (L)", "Volumen"),
        new Unidad("cm", "Centímetro (cm)", "Longitud"),
        new Unidad("m", "Metro (m)", "Longitud"),
        new Unidad("pieza", "Pieza", "Conteo y presentación"),
        new Unidad("par", "Par", "Conteo y presentación"),
        new Unidad("docena", "Docena", "Conteo y presentación"),
        new Unidad("caja", "Caja", "Conteo y presentación"),
        new Unidad("paquete", "Paquete", "Conteo y presentación"),
        new Unidad("bolsa", "Bolsa", "Conteo y presentación"),
        new Unidad("saco", "Saco", "Conteo y presentación"),
        new Unidad("frasco", "Frasco", "Conteo y presentación"),
        new Unidad("botella", "Botella", "Conteo y presentación"),
        new Unidad("rollo", "Rollo", "Conteo y presentación")
    });

    public static bool Contiene(string? valor) => Todas.Any(unidad => unidad.Valor == valor);
}
