using MySqlConnector;

namespace SGCRN.Data;

public sealed class DbConnectionFactory(IConfiguration configuration)
{
    public MySqlConnection Create()
    {
        var connectionString = configuration.GetConnectionString("SGCRN")
            ?? throw new InvalidOperationException("La cadena de conexión SGCRN no está configurada.");

        var connection = new MySqlConnection(connectionString);
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
