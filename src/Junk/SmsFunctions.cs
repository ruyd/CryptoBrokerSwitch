using System;
using Twilio.Clients;

namespace Piggy
{
    /// <summary>
    /// Estos credenciales estan con mi PayPal, no hagan party! 
    /// </summary>
    public static class SmsFunctions
    {
        public static string From { get; set; } = "17873";
        public static string SubscriptionID { get; set; } = "ACf6bf3ceb53d5e68469d80b22";
        public static string Token { get; set; } = "42e88564ec1a55ec84a4f3d9";

        public static void SendMessage(string to, string messageBody)
        {
            //var client = new Twilio.TwilioRestClient(SubscriptionID, Token);
            //client.SendMessage(From, to, messageBody);
            var client = new TwilioRestClient(SubscriptionID, Token);

            var message = Twilio.Rest.Api.V2010.Account.MessageResource.Create(
           to: new Twilio.Types.PhoneNumber(to),
           from: new Twilio.Types.PhoneNumber(From),
           body: messageBody,
           client: client);

            if (message.ErrorCode != null)
            {

            }

        }
        public static string GenerateConfirmCode(bool encryptResult = false)
        {
            var code = new Random().Next(100000, 999999).ToString();
            if (encryptResult)
            {
                code = AES.EncryptEx(code);
            }

            return code;
        }
    }
}