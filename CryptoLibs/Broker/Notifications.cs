using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneSignal.CSharp.SDK;

namespace Piggy
{
    public class OinkNotifications
    {
        public static void SendPushNofy(string body)
        {
            OneSignalClient client = new OneSignalClient("xxx");

            OneSignal.CSharp.SDK.Resources.Notifications.NotificationCreateOptions options = new OneSignal.CSharp.SDK.Resources.Notifications.NotificationCreateOptions();

            options.AppId = new Guid("73FCF214-4D50-4CFB-A3FE-5A3450ED9ED3");
            options.IncludedSegments = new List<string> { "All" };
            options.Contents.Add(OneSignal.CSharp.SDK.Resources.LanguageCodes.English, body);

            options.Data = new Dictionary<string, string>();

            client.Notifications.Create(options);
        }

    }
}
