namespace SGCRN.Data;

public sealed class MateriaPrimaDuplicadaException(Exception innerException)
    : Exception("El nombre de la materia prima ya está registrado.", innerException);
