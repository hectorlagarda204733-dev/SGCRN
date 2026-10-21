namespace SGCRN.Data;

public sealed class ProductoDuplicadoException(Exception innerException)
    : Exception("El almacenamiento rechazó un nombre de producto duplicado.", innerException);
