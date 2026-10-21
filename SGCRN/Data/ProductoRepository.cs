using MySqlConnector;
using SGCRN.Models;

namespace SGCRN.Data;

public sealed class ProductoRepository(DbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<ProductoTerminado>> GetAllAsync(string? buscar = null, bool? activo = null)
    {
        const string sql = """
            SELECT Id, Nombre, Descripcion, ImagenUrl, UnidadMedida, PrecioSugerido,
                   Activo, CreadoEn, ActualizadoEn
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
                   Activo, CreadoEn, ActualizadoEn
            FROM ProductosTerminados WHERE Id = @Id
            """;
        await using var connection = connectionFactory.Create();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<long> CreateAsync(ProductoTerminado producto)
    {
        const string sql = """
            INSERT INTO ProductosTerminados
                (Nombre, Descripcion, ImagenUrl, UnidadMedida, PrecioSugerido,
                 Activo, CreadoEn, ActualizadoEn)
            VALUES (@Nombre, @Descripcion, @ImagenUrl, @UnidadMedida, @PrecioSugerido,
                    @Activo, NOW(), NOW())
            """;
        try
        {
            await using var connection = connectionFactory.Create();
            await using var command = new MySqlCommand(sql, connection);
            AddProductParameters(command, producto);
            command.Parameters.AddWithValue("@Activo", producto.Activo);
            await command.ExecuteNonQueryAsync();
            return checked((long)command.LastInsertedId);
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new ProductoDuplicadoException(ex);
        }
    }

    public async Task<bool> UpdateAsync(ProductoTerminado producto)
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
            AddProductParameters(command, producto);
            command.Parameters.AddWithValue("@Id", producto.Id);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new ProductoDuplicadoException(ex);
        }
    }

    public async Task<bool> DeactivateAsync(long id)
    {
        const string sql = "UPDATE ProductosTerminados SET Activo = 0 WHERE Id = @Id AND Activo = 1";
        await using var connection = connectionFactory.Create();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static void AddProductParameters(MySqlCommand command, ProductoTerminado producto)
    {
        command.Parameters.AddWithValue("@Nombre", producto.Nombre);
        command.Parameters.AddWithValue("@Descripcion", (object?)producto.Descripcion ?? DBNull.Value);
        command.Parameters.AddWithValue("@ImagenUrl", (object?)producto.ImagenUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@UnidadMedida", producto.UnidadMedida);
        command.Parameters.AddWithValue("@PrecioSugerido", producto.PrecioSugerido);
    }

    private static ProductoTerminado Map(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt64("Id"),
        Nombre = reader.GetString("Nombre"),
        Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString("Descripcion"),
        ImagenUrl = reader.IsDBNull(reader.GetOrdinal("ImagenUrl")) ? null : reader.GetString("ImagenUrl"),
        UnidadMedida = reader.GetString("UnidadMedida"),
        PrecioSugerido = reader.GetDecimal("PrecioSugerido"),
        Activo = reader.GetBoolean("Activo"),
        CreadoEn = reader.GetDateTime("CreadoEn"),
        ActualizadoEn = reader.GetDateTime("ActualizadoEn")
    };
}
