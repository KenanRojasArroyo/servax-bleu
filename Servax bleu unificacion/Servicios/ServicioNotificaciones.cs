using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Servax_bleu_unificacion.Servicios
{
    public class ServicioNotificaciones
    {
        public static async Task EnviarCorreoAlertaAsync(string asunto, string mensajeHtml)
        {
            try
            {
                string host = ConfigurationManager.AppSettings["SmtpHost"];
                int port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                string user = ConfigurationManager.AppSettings["SmtpUser"];
                string pass = ConfigurationManager.AppSettings["SmtpPass"];
                string destino = ConfigurationManager.AppSettings["EmailAlertaDestino"];

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(user, "Sistema de Alertamiento - Servax Bleu");
                    mail.To.Add(destino);
                    mail.Subject = asunto;
                    mail.Body = mensajeHtml;
                    mail.IsBodyHtml = true;

                    using (var smtp = new SmtpClient(host, port))
                    {
                        smtp.Credentials = new NetworkCredential(user, pass);
                        smtp.EnableSsl = true;

                        await smtp.SendMailAsync(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al enviar correo: {ex.Message}");
            }
        }
    }
}