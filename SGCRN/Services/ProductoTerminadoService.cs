using MySqlConnector;
using SGCRN.Data;
using SGCRN.Models;

namespace SGCRN.Services;

public sealed class ProductoTerminadoService(DbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<ProductoTerminado>> GetAllAsync(string? buscar = null, bool? activo = null)
    {
        const string sql = """
            SELECT Id, Nombre, Descripcion, ImagenUrl, UnidadMedida, PrecioSugerido,
                   StockActual, StockMinimo, Activo, CreadoEn, ActualizadoEn
            FROM ProductosTerminados
            WHERE (@Buscar = '' OR LOCATE(@Buscar, Nombre) > 0)
              AND (@Activo IS NULL OR Activo = @Activo)
            ORDER BY Nombre
            """;
        await using var connection = connectionFactory.Create();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Buscar", buscar?.Trim() ?? "");
        command.Parameters.AddWithValue("@Activo", (object?)activo ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync();
        var productos = new List<ProductoTerminado>();
        while (await reader.ReadAsync()) productos.Add(Map(reader));
        return productos;
    }

    public async Task<ProductoTerminado?> GetByIdAsync(long id)
    {
        const string sql = """
            SELECT Id, Nombre, Descripcion, ImagenUrl, UnidadMedida, PrecioSugerido,
                   StockActual, StockMinimo, Activo, CreadoEn, ActualizadoEn
            FROM ProductosTerminados WHERE Id = @Id
            """;
        await using var connection = connectionFactory.Create();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<long> CreateAsync(ProductoTerminadoInput input)
    {
        const string sql = """
            INSERT INTO ProductosTerminados
                (Nombre, Descripcion, ImagenUrl, UnidadMedida, PrecioSugerido,
                 StockActual, StockMinimo, Activo, CreadoEn, ActualizadoEn)
            VALUES (@Nombre, @Descripcion, @ImagenUrl, @UnidadMedida, @PrecioSugerido,
                    0, 0, 1, NOW(), NOW())
            """;
        try
        {
            await using var connection = connectionFactory.Create();
            await using var command = new MySqlCommand(sql, connection);
            AddInputParameters(command, input);
            await command.ExecuteNonQueryAsync();
            return checked((long)command.LastInsertedId);
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw DuplicateNameException();
        }
    }

    public async Task UpdateAsync(long id, ProductoTerminadoInput input)
    {
        const string sql = """
            UPDATE ProductosTerminados
            SET Nombre = @Nombre, Descripcion = @Descripcion, ImagenUrl = @ImagenUrl,
                UnidadMedida = @UnidadMedida, PrecioSugerido = @PrecioSugerido
            WHERE Id = @Id
            """;
        try
        {
            await using var connection = connectionFactory.Create();
            await using var command = new MySqlCommand(sql, connection);
            AddInputParameters(command, input);
            command.Parameters.AddWithValue("@Id", id);
            if (await command.ExecuteNonQueryAsync() == 0)
                throw new InvalidOperationException("El producto ya no existe.");
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw DuplicateNameException();
        }
    }

    public async Task DeactivateAsync(long id)
    {
        const string sql = "UPDATE ProductosTerminados SET Activo = 0 WHERE Id = @Id AND Activo = 1";
        await using var connection = connectionFactory.Create();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        if (await command.ExecuteNonQueryAsync() == 0)
            throw new InvalidOperationException("El producto ya está inactivo o no existe.");
    }

    private static void AddInputParameters(MySqlCommand command, ProductoTerminadoInput input)
    {
        command.Parameters.AddWithValue("@Nombre", input.Nombre.Trim());
        command.Parameters.AddWithValue("@Descripcion", (object?)input.Descripcion?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ImagenUrl", (object?)input.ImagenUrl?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@UnidadMedida", input.UnidadMedida.Trim());
        command.Parameters.AddWithValue("@PrecioSugerido", input.PrecioSugerido);
    }

    private static ProductoTerminado Map(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt64("Id"),
        Nombre = reader.GetString("Nombre"),
        Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString("Descripcion"),
        ImagenUrl = reader.IsDBNull(reader.GetOrdinal("ImagenUrl")) ? null : reader.GetString("ImagenUrl"),
        UnidadMedida = reader.GetString("UnidadMedida"),
        PrecioSugerido = reader.GetDecimal("PrecioSugerido"),
        StockActual = reader.GetDecimal("StockActual"),
        StockMinimo = reader.GetDecimal("StockMinimo"),
        Activo = reader.GetBoolean("Activo"),
        CreadoEn = reader.GetDateTime("CreadoEn"),
        ActualizadoEn = reader.GetDateTime("ActualizadoEn")
    };

    private static InvalidOperationException DuplicateNameException() =>
        new("Ya existe un producto con ese nombre, incluso si está inactivo.");
}
