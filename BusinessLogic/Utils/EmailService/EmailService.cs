using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace BusinessLogic.Utils.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private string _smtpPassword;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _smtpHost = configuration["SmtpConfig:Host"];
            _smtpPort = int.Parse(configuration["SmtpConfig:Port"]);
            _smtpUsername = configuration["SmtpConfig:Username"];
            _smtpPassword = configuration["SmtpConfig:Password"];
            _logger = logger;
        }

        public async Task SendVerificationEmail(string toEmail, string emailVerificationLink)
        {
            try
            {
                var from = new MailAddress(_smtpUsername);
                var to = new MailAddress(toEmail);
                var subject = "Verify your email";
                string htmlBody = string.Empty;
                try
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    string templatePath = Path.Combine(
                        currentDirectory,
                        "../DataAccess/Utils/EmailService/Templates/verification_email_template_file.html"
                    );

                    string emailTemplate = File.ReadAllText(templatePath);
                    htmlBody = emailTemplate;
                    if (string.IsNullOrEmpty(emailTemplate))
                        throw new Exception();
                    htmlBody = htmlBody.Replace("{emailVerificationLink}", emailVerificationLink);
                }
                catch
                {
                    var body =
                        @"<!DOCTYPE html>
                                <html>
                                <head>
                                <meta charset='utf-8' />
                                <title>Email Confirmation</title>
                                </head>
                                <body>
                                <div style='background-color:#F5F5F5;padding:20px;'>
                                <h2>Email Confirmation</h2>
                                <p>Please click on the link below to confirm your email address:</p>
                                <a href='"
                        + emailVerificationLink
                        + @"' style='display:inline-block;background-color:#4CAF50;color:#FFFFFF;padding:8px 16px;text-decoration:none;border-radius:4px;'>Confirm Email Address</a>
                                <p>If you didn't request to confirm this email address, please disregard this message.</p>
                                </div>
                                </body>
                                </html>";

                    htmlBody = body;
                    htmlBody = htmlBody.Replace("{emailVerificationLink}", emailVerificationLink);
                }
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    smtpClient.EnableSsl = true;
                    using (var message = new MailMessage(from, to))
                    {
                        message.Subject = subject;
                        message.Body = htmlBody;
                        message.IsBodyHtml = true;
                        var cancellationTokenSource = new CancellationTokenSource(
                            TimeSpan.FromSeconds(30)
                        );
                        var cancellationToken = cancellationTokenSource.Token;
                        // Gửi email và chờ đợi trong thời gian chờ tối đa
                        await Task.WhenAny(
                            smtpClient.SendMailAsync(message),
                            Task.Delay(-1, cancellationToken)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            }
        }

        public async Task SendVerifyCodeToEmail(string toEmail, string VerifyCode)
        {
            if (string.IsNullOrEmpty(_smtpPassword))
            {
                _smtpPassword = "ymzoogjfgjjogmbx";
            }
            var htmlBody = "";
            try
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string templatePath = Path.Combine(
                    currentDirectory,
                    "../DataAccess/Utils/EmailService/Templates/verify_code_template.html"
                );

                string emailTemplate = File.ReadAllText(templatePath);
                htmlBody = emailTemplate;
                if (string.IsNullOrEmpty(emailTemplate))
                    throw new Exception();
                htmlBody = emailTemplate;
                htmlBody = htmlBody.Replace("{VerifyCode}", VerifyCode);
            }
            catch
            {
                htmlBody =
                    @"<!DOCTYPE html>
                            <html>
                            <head>
                                <meta charset=""UTF-8"">
                                <title>Verify Code</title>
                            </head>
                            <body>
                                <div style=""font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;"">
                                    <h2>Mã xác thực</h2>
                                    <p style=""font-size: 24px; font-weight: bold;"">{VerifyCode}</p>
                                    <br>
                                    <p>Best regards,</p>
                                    <p>[TuTam]</p>
                                </div>
                            </body>
                            </html>";
                htmlBody = VerifyCode;
                htmlBody = htmlBody.Replace("{VerifyCode}", VerifyCode);
            }
            try
            {
                // gửi email
                var from = new MailAddress(_smtpUsername);
                var to = new MailAddress(toEmail);
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    smtpClient.EnableSsl = true;

                    using (var message = new MailMessage(from, to))
                    {
                        message.Subject = "Verify code";
                        message.Body = htmlBody;
                        message.IsBodyHtml = true;

                        var cancellationTokenSource = new CancellationTokenSource(
                            TimeSpan.FromSeconds(30)
                        );
                        var cancellationToken = cancellationTokenSource.Token;

                        // Gửi email và chờ đợi trong thời gian chờ tối đa
                        await Task.WhenAny(
                            smtpClient.SendMailAsync(message),
                            Task.Delay(-1, cancellationToken)
                        );
                    }
                }
            }
            catch { }
        }

        public async Task SendNotificationAboutDenyCharity(
            string toEmail,
            string name,
            string reason
        )
        {
            try
            {
                var from = new MailAddress(_smtpUsername);
                var to = new MailAddress(toEmail);
                var subject =
                    "Thông báo về việc đăng ký tài khoản tổ chức từ thiện trên nền tảng Từ Tâm";
                string htmlBody = string.Empty;
                try
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    string templatePath = Path.Combine(
                        currentDirectory,
                        "../DataAccess/Utils/EmailService/Templates/creating_charity_email_template_file.html"
                    );

                    string emailTemplate = File.ReadAllText(templatePath);
                    htmlBody = emailTemplate;
                    if (string.IsNullOrEmpty(emailTemplate))
                        throw new Exception();
                    htmlBody = htmlBody.Replace("{reason}", reason);
                    htmlBody = htmlBody.Replace("{name}", name);
                }
                catch
                {
                    var body =
                        @"<!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Từ Chối Đăng Ký Tài Khoản</title>
                        </head>
                        <body>
                            <table width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td align=""center"" bgcolor=""#f4f4f4"">
                                        <table width=""600"" cellspacing=""0"" cellpadding=""0"">
                                            <tr>
                                                <td align=""center"" bgcolor=""#ffffff"" style=""padding: 20px;"">
                                                    <img src=""your-logo.png"" alt=""Your Logo"" width=""150"">
                                                    <h2>Kính gửi {name}</h2>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td bgcolor=""#ffffff"" style=""padding: 20px;"">
                                                    <p>Xin chào bạn,</p>
                                                    <p>Chúng tôi rất tiếc phải thông báo rằng yêu cầu đăng ký tài khoản của tổ chức từ thiện của bạn đã bị từ chối.</p>
                                                    <p>Lý do từ chối: {reason}</p>
                                                    <p>Nếu bạn có bất kỳ câu hỏi hoặc cần thêm thông tin, vui lòng liên hệ chúng tôi qua địa chỉ email TuTam@gmail.com.</p>
                                                    <p>Xin lỗi về sự bất tiện này.</p>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td bgcolor=""#ffffff"" style=""padding: 20px;"">
                                                    <p>Trân trọng,</p>
                                                    <p>TuTam</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </body>
                        </html>";

                    htmlBody = body;
                    htmlBody = htmlBody.Replace("{reason}", reason);
                    htmlBody = htmlBody.Replace("{name}", name);
                }
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    smtpClient.EnableSsl = true;
                    using (var message = new MailMessage(from, to))
                    {
                        message.Subject = subject;
                        message.Body = htmlBody;
                        message.IsBodyHtml = true;
                        var cancellationTokenSource = new CancellationTokenSource(
                            TimeSpan.FromSeconds(30)
                        );
                        var cancellationToken = cancellationTokenSource.Token;
                        // Gửi email và chờ đợi trong thời gian chờ tối đa
                        await Task.WhenAny(
                            smtpClient.SendMailAsync(message),
                            Task.Delay(-1, cancellationToken)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            }
        }

        public async Task SendNotificationForCreatingAccountForBranchAdminEmail(
            string toEmail,
            string userName,
            string phone,
            string password
        )
        {
            try
            {
                var from = new MailAddress(_smtpUsername);
                var to = new MailAddress(toEmail);
                var subject = "Thông báo về tài khoản đăng nhập của hệ thống từ thiện Từ Tâm";
                string htmlBody = string.Empty;
                try
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    string templatePath = Path.Combine(
                        currentDirectory,
                        "../DataAccess/Utils/EmailService/Templates/creating_account_email_template_file.html"
                    );

                    string emailTemplate = File.ReadAllText(templatePath);
                    if (string.IsNullOrEmpty(emailTemplate))
                        throw new Exception();
                    htmlBody = emailTemplate;
                    htmlBody = htmlBody.Replace("{userName}", userName);
                    htmlBody = htmlBody.Replace("{password}", password);
                    htmlBody = htmlBody.Replace("{phone}", phone);
                }
                catch
                {
                    var body =
                        @"<!DOCTYPE html>
                            <html>
                            <head>
                                <title>Thông báo: Tài khoản quản lý đã được tạo</title>
                            </head>
                            <body>
                                <p>Chào bạn,</p>
                                <p>Tài khoản quản lý đã được tạo bởi admin. Dưới đây là thông tin đăng nhập của bạn:</p>
                                <ul>
                                    <li><strong>Tên đăng nhập:</strong> {userName}</li>
                                    <li><strong>Hoặc</strong></li>
                                    <li><strong>Tên đăng nhập:</strong> {phone}</li>
                                    <li><strong>Mật khẩu:</strong> {password}</li>
                                </ul>
                                <p>Vui lòng tuân thủ các biện pháp bảo mật sau đây để bảo vệ tài khoản của bạn:</p>
                                <ul>
                                    <li><strong>1. Khuyến nghị đổi mật khẩu:</strong> Hãy đổi mật khẩu ngay sau khi đăng nhập lần đầu tiên để đảm bảo tính bảo mật cho tài khoản của bạn.</li>
                                    <li><strong>2. Mật khẩu mạnh:</strong> Sử dụng một mật khẩu mạnh chứa ít nhất 8 ký tự, bao gồm cả chữ hoa, chữ thường, số và ký tự đặc biệt.</li>
                                    <li><strong>3. Không chia sẻ mật khẩu:</strong> Không bao giờ chia sẻ mật khẩu của bạn với người khác và không ghi chú mật khẩu ở nơi dễ thấy.</li>
                                    <li><strong>4. Đăng xuất:</strong> Đảm bảo đăng xuất khi không sử dụng tài khoản để ngăn ngừa truy cập trái phép.</li>
                                </ul>
                                <p>Chúng tôi khuyến nghị bạn hãy thay đổi mật khẩu ngay sau khi nhận được email này.</p>
                                <p>Nếu bạn có bất kỳ câu hỏi hoặc cần sự hỗ trợ, vui lòng liên hệ với chúng tôi qua email support@example.com.</p>
                                <p>Trân trọng,</p>
                                <p>Admin</p>
                            </body>
                            </html>
                            ";

                    htmlBody = body;
                    htmlBody = htmlBody.Replace("{userName}", userName);
                    htmlBody = htmlBody.Replace("{password}", password);
                    htmlBody = htmlBody.Replace("{phone}", phone);
                }
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    smtpClient.EnableSsl = true;
                    using (var message = new MailMessage(from, to))
                    {
                        message.Subject = subject;
                        message.Body = htmlBody;
                        message.IsBodyHtml = true;
                        var cancellationTokenSource = new CancellationTokenSource(
                            TimeSpan.FromSeconds(30)
                        );
                        var cancellationToken = cancellationTokenSource.Token;
                        // Gửi email và chờ đợi trong thời gian chờ tối đa
                        await Task.WhenAny(
                            smtpClient.SendMailAsync(message),
                            Task.Delay(-1, cancellationToken)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            }
        }

        public async Task SendNotificationForDenyCharityUnitUpdateEmail(
            string toEmail,
            string name,
            string reason
        )
        {
            try
            {
                var from = new MailAddress(_smtpUsername);
                var to = new MailAddress(toEmail);
                var subject = "Thông báo về tài khoản đăng nhập của hệ thống từ thiện Từ Tâm";
                string htmlBody = string.Empty;
                try
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    string templatePath = Path.Combine(
                        currentDirectory,
                        "../DataAccess/Utils/EmailService/Templates/update_charity_email_template_file.html"
                    );

                    string emailTemplate = File.ReadAllText(templatePath);
                    htmlBody = emailTemplate;
                    if (string.IsNullOrEmpty(emailTemplate))
                        throw new Exception();
                    htmlBody = htmlBody.Replace("{reason}", reason);
                    htmlBody = htmlBody.Replace("{name}", name);
                }
                catch
                {
                    var body =
                        @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Từ Chối Đăng Ký Tài Khoản</title>
</head>
<body>
    <table width=""100%"" cellspacing=""0"" cellpadding=""0"">
        <tr>
            <td align=""center"" bgcolor=""#f4f4f4"">
                <table width=""600"" cellspacing=""0"" cellpadding=""0"">
                    <tr>
                        <td align=""center"" bgcolor=""#ffffff"" style=""padding: 20px;"">
                            <img src=""your-logo.png"" alt=""Your Logo"" width=""150"">
                            <h2>Từ Chối Đăng Ký Tài Khoản</h2>
                        </td>
                    </tr>
                    <tr>
                        <td bgcolor=""#ffffff"" style=""padding: 20px;"">
                            <p>Xin chào bạn,</p>
                            <p>Chúng tôi rất tiếc phải thông báo rằng yêu cầu xét duyệt thay đổi thông tin của bạn đã không được chấp nhận.</p>
                            <p>Lý do từ chối: {reason}</p>
                            <p>Nếu bạn có bất kỳ câu hỏi hoặc cần thêm thông tin, vui lòng liên hệ chúng tôi qua địa chỉ email TuTam@gmail.com.</p>
                            <p>Xin lỗi về sự bất tiện này.</p>
                        </td>
                    </tr>
                    <tr>
                        <td bgcolor=""#ffffff"" style=""padding: 20px;"">
                            <p>Trân trọng,</p>
                            <p>TuTam</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
";

                    htmlBody = body;
                    htmlBody = htmlBody.Replace("{reason}", reason);
                    htmlBody = htmlBody.Replace("{name}", name);
                }
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    smtpClient.EnableSsl = true;
                    using (var message = new MailMessage(from, to))
                    {
                        message.Subject = subject;
                        message.Body = htmlBody;
                        message.IsBodyHtml = true;
                        var cancellationTokenSource = new CancellationTokenSource(
                            TimeSpan.FromSeconds(30)
                        );
                        var cancellationToken = cancellationTokenSource.Token;
                        // Gửi email và chờ đợi trong thời gian chờ tối đa
                        await Task.WhenAny(
                            smtpClient.SendMailAsync(message),
                            Task.Delay(-1, cancellationToken)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            }
        }

        public async Task SendNotificationForCreatingAccountForCharityUnitEmail(
            string toEmail,
            string userName,
            string phone,
            string password
        )
        {
            try
            {
                var from = new MailAddress(_smtpUsername);
                var to = new MailAddress(toEmail);
                var subject = "Thông báo về tài khoản đăng nhập của hệ thống từ thiện Từ Tâm";
                string htmlBody = string.Empty;
                try
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    string templatePath = Path.Combine(
                        currentDirectory,
                        "../DataAccess/Utils/EmailService/Templates/creating_account_email_template_file.html"
                    );
                    string emailTemplate = File.ReadAllText(templatePath);
                    if (string.IsNullOrEmpty(emailTemplate))
                        throw new Exception();
                    htmlBody = emailTemplate;
                    htmlBody = htmlBody.Replace("{userName}", userName);
                    htmlBody = htmlBody.Replace("{password}", password);
                    htmlBody = htmlBody.Replace("{phone}", phone);
                }
                catch
                {
                    var body =
                        @"<!DOCTYPE html>
                            <html>
                            <head>
                                <title>Thông báo: Tài khoản quản lý đã được tạo</title>
                            </head>
                            <body>
                                <p>Chào bạn,</p>
                                <p>Tài khoản quản lý đã được tạo bởi admin. Dưới đây là thông tin đăng nhập của bạn:</p>
                                <ul>
                                     <li><strong>Tên đăng nhập:</strong> {userName}</li>
                                      <li><strong>Hoặc</strong></li>
                                      <li><strong>Tên đăng nhập:</strong> {phone}</li>
                                      <li><strong>Mật khẩu:</strong> {password}</li>
                                </ul>
                                <p>Vui lòng tuân thủ các biện pháp bảo mật sau đây để bảo vệ tài khoản của bạn:</p>
                                <ul>
                                    <li><strong>1. Khuyến nghị đổi mật khẩu:</strong> Hãy đổi mật khẩu ngay sau khi đăng nhập lần đầu tiên để đảm bảo tính bảo mật cho tài khoản của bạn.</li>
                                    <li><strong>2. Mật khẩu mạnh:</strong> Sử dụng một mật khẩu mạnh chứa ít nhất 8 ký tự, bao gồm cả chữ hoa, chữ thường, số và ký tự đặc biệt.</li>
                                    <li><strong>3. Không chia sẻ mật khẩu:</strong> Không bao giờ chia sẻ mật khẩu của bạn với người khác và không ghi chú mật khẩu ở nơi dễ thấy.</li>
                                    <li><strong>4. Đăng xuất:</strong> Đảm bảo đăng xuất khi không sử dụng tài khoản để ngăn ngừa truy cập trái phép.</li>
                                </ul>
                                <p>Chúng tôi khuyến nghị bạn hãy thay đổi mật khẩu ngay sau khi nhận được email này.</p>
                                <p>Nếu bạn có bất kỳ câu hỏi hoặc cần sự hỗ trợ, vui lòng liên hệ với chúng tôi qua email support@example.com.</p>
                                <p>Trân trọng,</p>
                                <p>Admin</p>
                            </body>
                            </html>
                            ";

                    htmlBody = body;
                    htmlBody = htmlBody.Replace("{userName}", userName);
                    htmlBody = htmlBody.Replace("{password}", password);
                    htmlBody = htmlBody.Replace("{phone}", phone);
                }
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    smtpClient.EnableSsl = true;
                    using (var message = new MailMessage(from, to))
                    {
                        message.Subject = subject;
                        message.Body = htmlBody;
                        message.IsBodyHtml = true;
                        var cancellationTokenSource = new CancellationTokenSource(
                            TimeSpan.FromSeconds(30)
                        );
                        var cancellationToken = cancellationTokenSource.Token;
                        // Gửi email và chờ đợi trong thời gian chờ tối đa
                        await Task.WhenAny(
                            smtpClient.SendMailAsync(message),
                            Task.Delay(-1, cancellationToken)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            }
        }
    }
}
