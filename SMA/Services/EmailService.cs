using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SMA.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _senderEmail = "naikivietnam@gmail.com"; // Thay bằng email của bạn
        private readonly string _senderPassword = "uenh bott qoqo cgwx"; // Thay bằng app password

        public async Task<bool> SendOtpEmailAsync(string recipientEmail, string otp)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_senderEmail, _senderPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_senderEmail, "Sale Management System"),
                        Subject = "Password Reset OTP",
                        Body = GenerateOtpEmailBody(otp),
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(recipientEmail);

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log error (implement logging as needed)
                Console.WriteLine($"Failed to send email: {ex.Message}");
                return false;
            }
        }

        private string GenerateOtpEmailBody(string otp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f3f4f6;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: white;
            border-radius: 8px;
            padding: 40px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .logo {{
            width: 60px;
            height: 60px;
            background-color: #3B82F6;
            border-radius: 50%;
            display: inline-block;
            line-height: 60px;
            color: white;
            font-size: 30px;
            margin-bottom: 20px;
        }}
        h1 {{
            color: #1F2937;
            font-size: 24px;
            margin: 0;
        }}
        .otp-box {{
            background-color: #F3F4F6;
            border: 2px dashed #3B82F6;
            border-radius: 8px;
            padding: 20px;
            text-align: center;
            margin: 30px 0;
        }}
        .otp {{
            font-size: 36px;
            font-weight: bold;
            color: #3B82F6;
            letter-spacing: 8px;
        }}
        .message {{
            color: #374151;
            line-height: 1.6;
            margin: 20px 0;
        }}
        .warning {{
            background-color: #FEF3C7;
            border-left: 4px solid #F59E0B;
            padding: 12px;
            margin: 20px 0;
            color: #92400E;
        }}
        .footer {{
            text-align: center;
            color: #6B7280;
            font-size: 12px;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #E5E7EB;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🔒</div>
            <h1>Password Reset Request</h1>
        </div>
        
        <p class='message'>
            You have requested to reset your password for the Sale Management System.
            Please use the following OTP code to proceed:
        </p>
        
        <div class='otp-box'>
            <div class='otp'>{otp}</div>
        </div>
        
        <p class='message'>
            This OTP is valid for <strong>5 minutes</strong> and can be used only once.
        </p>
        
        <div class='warning'>
            ⚠ If you did not request this password reset, please ignore this email and ensure your account is secure.
        </div>
        
        <div class='footer'>
            <p>© 2025 Sale Management System. All rights reserved.</p>
            <p>This is an automated email. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
