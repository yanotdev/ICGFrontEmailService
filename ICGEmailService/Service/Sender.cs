using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ICGEmailService.Service
{
    public class Sender
    {
        private static IConfiguration? _configuration;
        private static ILogger<Worker> _logger;
        private static string? _senderEmail;
        private static string? _senderEmailPassword;
        private static string? _smtpHost;
        private static string? _companyName;
        private static int _smtpPort;
        private static bool _smtpSslValue;

        public Sender(IConfiguration configuration, ILogger<Worker> logger)
        {
            _configuration = configuration;
            _senderEmail = _configuration["SenderEmail"];
            _senderEmailPassword = _configuration["SenderEmailPassword"];
            _smtpHost = _configuration["SmtpHost"];
            _smtpPort = Convert.ToInt32(_configuration["SmtpPort"]);
            _smtpSslValue = Convert.ToBoolean(_configuration["SmtpSsl"]);
            _companyName = _configuration["CompanyName"];
            _logger = logger;
        }

        public static async Task<bool> SendMail(string receiptEmail, string Subject, string messageBody)
        {
            try
            {

                SmtpClient smtp = new()
                {
                    Credentials = new NetworkCredential(_senderEmail, _senderEmailPassword),
                    Port = _smtpPort,
                    Host = _smtpHost,
                    EnableSsl = _smtpSslValue
                };

                MailMessage message = new MailMessage();

                message.From = new MailAddress($"{_companyName} <{_senderEmail}>");
                message.To.Add(receiptEmail);
                message.Subject = Subject;
                message.Body = messageBody;
                message.IsBodyHtml = true;
                await smtp.SendMailAsync(message);
                _logger.LogError($"sending email..");
            }
            catch (Exception)
            {
                _logger.LogError($"failed to send");
                throw;
            }


            return true;
        }
    }
}
