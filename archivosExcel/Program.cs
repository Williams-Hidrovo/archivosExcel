using System.Text;
using DotNetEnv;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;


class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var dbSettings = configuration.GetSection("DatabaseSettings");
        string server = dbSettings["DB_SERVER"];
        string database = dbSettings["DB_NAME"];
        string user = dbSettings["DB_USER"];
        string DBpassword = dbSettings["DB_PASSWORD"];
        string directoryPath = dbSettings["DIRECTORY_PATH"];
        // Construir la cadena de conexión a partir de las variables de entorno
        string connectionString = $"Server={server};Database={database};User Id={user};Password={DBpassword};TrustServerCertificate=True;MultipleActiveResultSets=true";
        LogService logService = new LogService(connectionString);

        // Reemplazar el marcador con la raíz del proyecto
        if (directoryPath.Contains("%ProjectRoot%"))
        {
            directoryPath = directoryPath.Replace("%ProjectRoot%", AppDomain.CurrentDomain.BaseDirectory);
        }

        // Parámetro para el StoredProcedure
        short informeId = 1; // Cambia este valor según lo que necesites

        string query = "SELECT TOP 100 * FROM ACC_ASIENTOS_SEARCH";
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileNameCsv = $"output_{timestamp}.csv";
        string filePath = Path.Combine(directoryPath, fileNameCsv);

        try
        {
            await using (SqlConnection connection = new SqlConnection(connectionString))
            {
                //SqlCommand command = new SqlCommand(query, connection);

                //comando para ejecutar el StoredProcedure
                SqlCommand command = new SqlCommand("dbo.SP_CerveceriaNacionalInformes", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@informeId", informeId);

                await connection.OpenAsync();
                await using SqlDataReader reader = await command.ExecuteReaderAsync();

                // Escribir los resultados en archivo CSV
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

            // Leer configuración de EmailSettings
            var emailSettings = configuration.GetSection("EmailSettings");
            string smtpServer = emailSettings["SmtpServer"];
            int port = int.Parse(emailSettings["Port"]);
            string fromEmail = emailSettings["FromEmail"];
            string password = emailSettings["Password"];
            var toRecipients = emailSettings.GetSection("ToRecipients").Get<List<string>>();
            var ccRecipients = emailSettings.GetSection("CcRecipients").Get<List<string>>();
            var bccRecipients = emailSettings.GetSection("BccRecipients").Get<List<string>>();

            string attachmentPath = Path.Combine(Directory.GetCurrentDirectory(), fileNameCsv);

            // Crear instancia del servicio
            EmailService emailService = new EmailService(smtpServer, port, fromEmail, password, logService);

            // Datos del correo
            string fecha = DateTime.Now.ToString("dd_MM_yyyy");
            string subject = $"input_tracker_ec_{fecha}";
            string body = "Informe cliente cervecería nacional desde el 2024 a la fecha.";

            //enviar el correo
            if (File.Exists(attachmentPath))
            {
                emailService.SendEmail(subject, body, toRecipients, attachmentPath);
                Console.WriteLine("Correo enviado exitosamente.");
            }
            else
            {
                logService.Log("El archivo CSV no se encontró. No se envió el correo.");
            }
        }
        catch (Exception ex)
        {
            logService.Log($"Error en Program.cs: {ex.Message}");
        }

        
    }
}
