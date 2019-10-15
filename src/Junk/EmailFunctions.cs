using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PigSwitch 
{
    public static class EmailFunctions
    {
        public static bool SendNotification(string email, string name, string subject, string body)
        {
            var result = false;

            var message = new System.Net.Mail.MailMessage();
            message.To.Add(email);
            message.Subject = subject;
            message.From = new System.Net.Mail.MailAddress("no-reply@ ", "piggy");
            message.IsBodyHtml = true;

            var templatesDir = HttpContext.Current.Server.MapPath("Templates/Email");
            var defaultTemplate = System.IO.Path.Combine(templatesDir, "default.html");
            if (System.IO.File.Exists(defaultTemplate))
            {
                var templateText = System.IO.File.ReadAllText(defaultTemplate);
                templateText = templateText.Replace("[Subject]", subject);
                templateText = templateText.Replace("[Body]", body);
                templateText = templateText.Replace("[GreetingName]", name);
            }
            else
                message.Body = @"<!DOCTYPE html><head><style type=""text/css""></style></head><body style=""margin: 0; font-family: Arial; font-size: 80%;""><h2>" + subject + "</h2><p>" + body + "</p></div></body></html>";

            var smtp = new System.Net.Mail.SmtpClient();

            try
            {
                //smtp.Send(message);
                result = true;
            }
            catch (Exception ex)
            {
                var s = ex.ToString();
            }

            return result;
        }

        public static void SendFailedLoginAttempt(dynamic request)
        {
            Task.Factory.StartNew(() =>
            {
                var bodyText = "<p>A new connection to your account (" + request.username + ") is being made from another (" + request.device?.TypeName + ")." +
                " If it's you please use another device (ie: computer or tablet or mobile). " +
                " If this activity is suspicious please report it to us. </p>" +
                " <ul><li><b>IP Address:</b> " + request.device?.IPAddress + "</li>" +
                " <li><b>Location:</b> " + request.device?.Geo?.CityLine + "</li>" +
                " <li><b>Device:</b> " + request.device?.TypeName + "</li>" +
                " <li><b>Internet Provider:</b> " + request.device?.ISP + "</li></ul>";

                var mailClient = new System.Net.Mail.SmtpClient();
                var from = new System.Net.Mail.MailAddress("no-reply@piggy.com", "");
                var mailMessage = new System.Net.Mail.MailMessage();
                mailMessage.Subject = "Suspicious Activity";

                mailMessage.Body = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath("~/ServerTemplates/") + "Default.html");
                mailMessage.Body = mailMessage.Body.Replace("[Heading]", "Suspicious Activity");
                mailMessage.Body = mailMessage.Body.Replace("[Title]", "");
                mailMessage.Body = mailMessage.Body.Replace("[Body]", bodyText);

                mailMessage.IsBodyHtml = true;
                mailMessage.From = from;

                mailMessage.To.Add("rdelgado@piggy.com");
        //mailMessage.To.Add(request.username);

        try
                {
                    mailClient.Send(mailMessage);
                }
                catch (Exception ex)
                {

                }
            });
        }

        public static bool SendRegistrationActivation(string email, string token, dynamic device = null, string templateFile = "RegisterActivationEmail.html")
        {
            var mailClient = new System.Net.Mail.SmtpClient();
            var from = new System.Net.Mail.MailAddress("no-reply@piggy.com", "piggy");
            var mailMessage = new System.Net.Mail.MailMessage();
            mailMessage.Subject = "Action Required: Please Confirm Email";

            mailMessage.Body = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath("~/ServerTemplates/") + templateFile);

            mailMessage.Body = mailMessage.Body.Replace("[date]", DateTime.UtcNow.AddHours(-4).ToString());
            if (device != null && !device.IsLoaded)
                device.Load();

            mailMessage.Body = mailMessage.Body.Replace("[ip]", device?.IPAddress);
            mailMessage.Body = mailMessage.Body.Replace("[isp]", device?.ISP);
            mailMessage.Body = mailMessage.Body.Replace("[location]", device?.Geo?.CityLine);

            var url = "https://www.piggy.com/home/activate?token=" + token;
            if (Environment.MachineName == "RDTCWS01")
                url = "http://localhost:49617/home/activate?token=" + token;

            mailMessage.Body = mailMessage.Body.Replace("[url]", url);

            mailMessage.IsBodyHtml = true;
            mailMessage.From = from;
            mailMessage.To.Add(email);

            //mailMessage.To.Add("ruydelgado@gmail.com");

            try
            {
                mailClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public static bool SendForgotPassword(string email, string code, dynamic device)
        {
            var mailClient = new System.Net.Mail.SmtpClient();
            var from = new System.Net.Mail.MailAddress("no-reply@piggy.com", "Account");
            var mailMessage = new System.Net.Mail.MailMessage();
            mailMessage.Subject = "Action Required: piggy Code";

            mailMessage.Body = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath("~/ServerTemplates/") + "ForgotPasswordEmail.html");

            mailMessage.Body = mailMessage.Body.Replace("[date]", DateTime.UtcNow.AddHours(-4).ToString());
            if (device != null && !device.IsLoaded)
                device.Load();

            mailMessage.Body = mailMessage.Body.Replace("[ip]", device?.IPAddress);
            mailMessage.Body = mailMessage.Body.Replace("[isp]", device?.ISP);
            mailMessage.Body = mailMessage.Body.Replace("[location]", device?.Geo?.CityLine);

            mailMessage.Body = mailMessage.Body.Replace("[code]", code);

            mailMessage.IsBodyHtml = true;
            mailMessage.From = from;
            mailMessage.To.Add(email);

            try
            {
                mailClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

    }
}