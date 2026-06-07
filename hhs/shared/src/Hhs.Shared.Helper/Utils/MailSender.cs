using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Hhs.Shared.Helper.Utils;

public interface IMailSender
{
    Task SendEmailAsync(EmailRequest mailRequest);
}

public sealed class MailSender : IMailSender
{
    private readonly EmailConfiguration _emailConfiguration;

    public MailSender(IOptions<EmailConfiguration> emailConfiguration)
    {
        _emailConfiguration = emailConfiguration?.Value;
    }

    public async Task SendEmailAsync(EmailRequest mailRequest)
    {
        var email = new MimeMessage();
        email.Sender = MailboxAddress.Parse(_emailConfiguration.Mail);
        email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
        email.Subject = mailRequest.Subject;

        var builder = new BodyBuilder();
        // if (mailRequest.Attachments != null)
        // {
        //     foreach (var file in mailRequest.Attachments.Where(file => file.Length > 0))
        //     {
        //         byte[] fileBytes;
        //         using (var ms = new MemoryStream())
        //         {
        //             await file.CopyToAsync(ms);
        //             fileBytes = ms.ToArray();
        //         }
        //
        //         builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
        //     }
        // }

        builder.HtmlBody = mailRequest.Body;
        email.Body = builder.ToMessageBody();

        using (var smtp = new SmtpClient())
        {
            try
            {
                await smtp.ConnectAsync(_emailConfiguration.Host, _emailConfiguration.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailConfiguration.Mail, _emailConfiguration.Password);
                await smtp.SendAsync(email);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}

public class EmailConfiguration
{
    public string Mail { get; set; }
    public string DisplayName { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
}

public sealed class EmailRequest
{
    public string ToEmail { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    // public List<IFormFile> Attachments { get; set; }
}