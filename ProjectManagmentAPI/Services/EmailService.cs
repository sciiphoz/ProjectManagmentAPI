using ProjectManagementAPI.Interfaces;
using System.Net;
using System.Net.Mail;

namespace ProjectManagementAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetCodeAsync(string email, string code)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Smtp");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"] ?? "Project Management System";

                using var client = new SmtpClient(host, port);
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(username, password);

                var subject = "Восстановление пароля";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #333;'>Восстановление пароля</h2>
                            <p>Здравствуйте!</p>
                            <p>Вы запросили восстановление пароля. Ваш код подтверждения:</p>
                            <div style='text-align: center; padding: 15px; margin: 20px 0; background-color: #f0f0f0; border-radius: 5px;'>
                                <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px;'>{code}</span>
                            </div>
                            <p>Введите этот код на странице восстановления пароля. Код действителен в течение 15 минут.</p>
                            <p>Если вы не запрашивали восстановление пароля, просто проигнорируйте это письмо.</p>
                            <hr style='margin: 20px 0;' />
                            <p style='color: #888; font-size: 12px;'>С уважением,<br/>Команда Project Management System</p>
                        </div>
                    </body>
                    </html>";

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(email);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Письмо с кодом сброса пароля отправлено на {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки письма на {email}");
                throw;
            }
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Smtp");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"] ?? "Project Management System";

                using var client = new SmtpClient(host, port);
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(username, password);

                var subject = "Подтверждение email";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #333;'>Подтверждение email</h2>
                            <p>Здравствуйте!</p>
                            <p>Для завершения регистрации, пожалуйста, подтвердите ваш email:</p>
                            <div style='text-align: center; margin: 20px 0;'>
                                <a href='{confirmationLink}' style='display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;'>Подтвердить email</a>
                            </div>
                            <p>Или скопируйте ссылку в браузер:</p>
                            <p style='word-break: break-all;'>{confirmationLink}</p>
                            <hr style='margin: 20px 0;' />
                            <p style='color: #888; font-size: 12px;'>С уважением,<br/>Команда Project Management System</p>
                        </div>
                    </body>
                    </html>";

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(email);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Письмо с подтверждением email отправлено на {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки письма на {email}");
                throw;
            }
        }
        public async Task SendProjectInvitationAsync(string email, string projectName, string invitationLink)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Smtp");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"] ?? "Project Management System";

                using var client = new SmtpClient(host, port);
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(username, password);

                var subject = $"Приглашение в проект «{projectName}»";
                var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #333;'>Приглашение в проект</h2>
                    <p>Здравствуйте!</p>
                    <p>Вас пригласили присоединиться к проекту <strong>«{projectName}»</strong>.</p>
                    <p>Для принятия приглашения перейдите по ссылке:</p>
                    <div style='text-align: center; margin: 20px 0;'>
                        <a href='{invitationLink}' style='display: inline-block; padding: 12px 24px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px;'>Принять приглашение</a>
                    </div>
                    <p>Или скопируйте ссылку в браузер:</p>
                    <p style='word-break: break-all;'>{invitationLink}</p>
                    <p style='color: #888;'>Срок действия ссылки — 7 дней.</p>
                    <hr style='margin: 20px 0;' />
                    <p style='color: #888; font-size: 12px;'>С уважением,<br/>Команда Project Management System</p>
                </div>
            </body>
            </html>";

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(email);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Приглашение в проект отправлено на {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки приглашения на {email}");
                throw;
            }
        }
    }
}