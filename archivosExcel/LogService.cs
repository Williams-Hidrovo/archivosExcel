using System;
using Microsoft.Data.SqlClient;

public class LogService
{
    private readonly string _connectionString;

    public LogService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Log(string message)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "INSERT INTO LogsProcesosAutomaticos (fechaEjecucion, Observaciones, Aplicacion) VALUES (@fechaEjecucion, @Observaciones, @Aplicacion)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@fechaEjecucion", DateTime.Now);
                    command.Parameters.AddWithValue("@Observaciones", message);
                    command.Parameters.AddWithValue("@Aplicacion", "Estatus Cerveceria en Linea"); // Reemplaza "MiAplicacion" por el valor que corresponda
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al registrar el log: {ex.Message}");
        }
    }
}
