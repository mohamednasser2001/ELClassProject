using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;


namespace Models.Utilites
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                // هنا بيانات الاعتماد الخاصة بك صحيحة (App Password)
                Credentials = new NetworkCredential("mmm.zzz155mody@gmail.com", "eaue lvcd rjrq cbvv")
            };

            return client.SendMailAsync(
                new MailMessage(from: "mmm.zzz155mody@gmail.com", 
                                to: email,
                                subject: subject,
                                body: htmlMessage
                                )
                {
                    IsBodyHtml = true
                });
        }
    }
}
