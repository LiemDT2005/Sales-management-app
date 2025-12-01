using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using SMA.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMA.Services
{
    class SendInvoiceEmail
    {
        private readonly IConfiguration _config;

        public SendInvoiceEmail()
        {
            // 🔹 Load cấu hình từ file appsettings.json (auto reload)
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
        public async Task SendEmailToCustomer(string customerId, int orderId)
        {
            try
            {
                using var context = new Prn212G3Context();

                // Lấy thông tin đơn hàng + khách hàng + sản phẩm
                var order = await context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId);

                if (order == null)
                {
                    MessageBox.Show("Order or Customer not found.");
                    return;
                }

                var customer = order.Customer;
                var staff = order.User;

                // Bắt đầu tạo nội dung email HTML
                var sb = new StringBuilder();

                sb.AppendLine("<html>");
                sb.AppendLine("<body style='font-family:Arial, sans-serif;'>");
                sb.AppendLine($"<h2 style='text-align:center; color:#2d3436;'>INVOICE #{order.OrderId}</h2>");
                sb.AppendLine("<hr>");

                sb.AppendLine("<h3>Order Information</h3>");
                sb.AppendLine("<table style='width:100%; border-collapse:collapse;'>");
                sb.AppendLine($"<tr><td><b>Order ID:</b></td><td>{order.OrderId}</td></tr>");
                sb.AppendLine($"<tr><td><b>Customer:</b></td><td>{customer.CustomerName}</td></tr>");
                sb.AppendLine($"<tr><td><b>Staff:</b></td><td>{staff?.UserName ?? "N/A"}</td></tr>");
                sb.AppendLine($"<tr><td><b>Created At:</b></td><td>{order.CreatedAt:dd/MM/yyyy HH:mm}</td></tr>");
                sb.AppendLine("</table>");

                sb.AppendLine("<br><h3>Order Details</h3>");
                sb.AppendLine("<table style='width:100%; border-collapse:collapse; border:1px solid #ddd;'>");
                sb.AppendLine("<thead>");
                sb.AppendLine("<tr style='background-color:#f2f2f2; text-align:left;'>");
                sb.AppendLine("<th style='padding:8px; border:1px solid #ddd;'>Product</th>");
                sb.AppendLine("<th style='padding:8px; border:1px solid #ddd;'>Qty</th>");
                sb.AppendLine("<th style='padding:8px; border:1px solid #ddd;'>Unit Price</th>");
                sb.AppendLine("<th style='padding:8px; border:1px solid #ddd;'>Total</th>");
                sb.AppendLine("</tr>");
                sb.AppendLine("</thead>");
                sb.AppendLine("<tbody>");

                foreach (var detail in order.OrderDetails)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td style='padding:8px; border:1px solid #ddd;'>{detail.Product.ProductName}</td>");
                    sb.AppendLine($"<td style='padding:8px; border:1px solid #ddd;'>{detail.Quantity}</td>");
                    sb.AppendLine($"<td style='padding:8px; border:1px solid #ddd;'>{detail.Product.Price:N0} ₫</td>");
                    sb.AppendLine($"<td style='padding:8px; border:1px solid #ddd;'>{order.TotalPrice:N0} ₫</td>");
                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");

                sb.AppendLine("<br>");
                sb.AppendLine("<hr>");
                sb.AppendLine("<table style='width:100%;'>");
                sb.AppendLine($"<tr><td style='text-align:right;'><b>Total Price:</b> {order.TotalPrice:N0} ₫</td></tr>");
                sb.AppendLine($"<tr><td style='text-align:right;'>Points Used: {order.PointUsed}</td></tr>");
                sb.AppendLine($"<tr><td style='text-align:right; color:green;'>Points Received: {order.PointReceived}</td></tr>");
                sb.AppendLine("</table>");

                sb.AppendLine("<br><p>We appreciate your business!</p>");
                sb.AppendLine("<p>Best regards,<br><b>SMA Shop</b></p>");
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                string subject = $"[SMA Shop] Invoice #{order.OrderId}";
                string body = sb.ToString();

                await SendEmailAsync(customer.Email, subject, body);

                MessageBox.Show($"Email sent successfully to {customer.Email}", "Email Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending email: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task SendEmailAsync(string? toEmail, string subject, string body)
        {
            var smtpSection = _config.GetSection("SmtpSettings");
            string host = smtpSection["Host"];
            int port = int.Parse(smtpSection["Port"]);
            bool enableSsl = bool.Parse(smtpSection["EnableSsl"]);
            string fromEmail = smtpSection["Email"];
            string appPassword = smtpSection["AppPassword"];

            using var message = new MailMessage(fromEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(fromEmail, appPassword)
            };

            await smtpClient.SendMailAsync(message);

        }
    }
}
