using System.ComponentModel.DataAnnotations;
using SGCRN.Data;
using SGCRN.Models;

namespace SGCRN.Services;

public sealed class MateriaPrimaService(MateriaPrimaRepository materias)
{
    public Task<IReadOnlyList<MateriaPrima>> GetAllAsync(string? buscar = null, bool? activo = null) =>
        materias.GetAllAsync(buscar?.Trim(), activo);

    public Task<MateriaPrima?> GetByIdAsync(long id) => materias.GetByIdAsync(id);

    public async Task<long> CreateAsync(MateriaPrimaInput input)
    {
        var materia = BuildMateria(input);
        materia.Activo = true;
        materia.StockActual = 0;
        materia.StockMinimo = 0;

        try
        {
            return await materias.CreateAsync(materia);
        }
        catch (MateriaPrimaDuplicadaException)
        {
            throw DuplicateNameException();
        }
    }

    public async Task UpdateAsync(long id, MateriaPrimaInput input)
    {
        var materia = BuildMateria(input);
        materia.Id = id;
        try
        {
            if (!await materias.UpdateAsync(materia))
                throw new InvalidOperationException("La materia prima ya no existe.");
        }
        catch (MateriaPrimaDuplicadaException)
        {
            throw DuplicateNameException();
        }
    }

    public async Task DeactivateAsync(long id)
    {
        if (!await materias.DeactivateAsync(id))
            throw new InvalidOperationException("La materia prima ya está inactiva o no existe.");
    }

    private static MateriaPrima BuildMateria(MateriaPrimaInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), validateAllProperties: true);
        return new MateriaPrima
        {
            Nombre = input.Nombre.Trim(),
            Descripcion = input.Descripcion?.Trim(),
            UnidadMedida = input.UnidadMedida.Trim()
        };
    }

    private static InvalidOperationException DuplicateNameException() =>
        new("Ya existe una materia prima con ese nombre, incluso si está inactiva.");
}
