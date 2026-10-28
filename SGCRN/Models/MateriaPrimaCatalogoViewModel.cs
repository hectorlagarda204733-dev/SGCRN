namespace SGCRN.Models;

public class MateriaPrimaCatalogoViewModel
{
    public string? Buscar { get; set; }
    public bool? Activo { get; set; }
    public IReadOnlyList<MateriaPrima> MateriasPrimas { get; set; } = [];
}
