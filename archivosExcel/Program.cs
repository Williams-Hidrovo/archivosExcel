using System.Text;
using DotNetEnv;
using Microsoft.Data.SqlClient;

class Program
{
    static async Task Main(string[] args)
    {
        Env.Load();

        // Construir la cadena de conexión a partir de las variables de entorno
        string server = Environment.GetEnvironmentVariable("DB_SERVER");
        Console.WriteLine("este es tu server: ",server);
        string database = Environment.GetEnvironmentVariable("DB_NAME");
        string user = Environment.GetEnvironmentVariable("DB_USER");
        string password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        string connectionString = $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate=True;MultipleActiveResultSets=true";
        string directoryPath = Environment.GetEnvironmentVariable("DIRECTORY_PATH");
        //string directoryPath = @"C:\Users\desarrollo.12\Downloads\";

        string query = "SELECT TOP 100 * FROM ACC_ASIENTOS_SEARCH";
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(directoryPath, $"output_{timestamp}.csv");

        try
        {
            await using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                await using SqlDataReader reader = await command.ExecuteReaderAsync();

                await using StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8);

                // Escribir los encabezados de las columnas
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    await writer.WriteAsync(reader.GetName(i));
                    if (i < reader.FieldCount - 1)
                        await writer.WriteAsync(";");
                }
                await writer.WriteLineAsync();

                // Escribir los datos de las filas
                while (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        await writer.WriteAsync(reader.GetValue(i).ToString());
                        if (i < reader.FieldCount - 1)
                            await writer.WriteAsync(";");
                    }
                    await writer.WriteLineAsync();
                }
            }

            Console.WriteLine($"Datos exportados exitosamente a {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocurrió un error: {ex.Message}");
        }
    }
}
