using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.IO;

public class EmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;
    private readonly LogService _logService;

    public EmailService(string smtpServer, int port, string fromEmail, string password, LogService logService)
    {
        _fromEmail = fromEmail;
        _logService = logService;

        _smtpClient = new SmtpClient(smtpServer)
        {
            Port = port,
            Credentials = new NetworkCredential(fromEmail, password),
            EnableSsl = true
        };
    }

    public void SendEmail(string subject, string body, IEnumerable<string> toRecipients, string attachmentPath, IEnumerable<string> ccRecipients = null, IEnumerable<string> bccRecipients = null)
    {
        try
        {
            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var recipient in toRecipients)
            {
                mailMessage.To.Add(recipient);
            }

            if (ccRecipients != null)
            {
                foreach (var recipient in ccRecipients)
                {
                    mailMessage.CC.Add(recipient);
                }
            }

            if (bccRecipients != null)
            {
                foreach (var recipient in bccRecipients)
                {
                    mailMessage.Bcc.Add(recipient);
                }
            }

            // Verificar si el archivo existe y agregarlo como adjunto
            if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
            {
                mailMessage.Attachments.Add(new Attachment(attachmentPath));
            }
            else if (!string.IsNullOrWhiteSpace(attachmentPath))
            {
                Console.WriteLine($"El archivo '{attachmentPath}' no existe.");
            }

            _smtpClient.Send(mailMessage);
            // Registrar el log de éxito
            _logService.Log("Proceso exitoso: El correo fue enviado correctamente.");
        }
        catch (Exception ex)
        {
            // Registrar el log del error
            _logService.Log($"Error al enviar el correo: {ex.Message}");
        }
    }
}
