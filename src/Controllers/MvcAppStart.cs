using Piggy;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
 
  
namespace PigSwitch
{
    public class DBCacheConn
    {
        public string PK_SignalR { get; set; }
        public string MAC { get; set; }
        public string LocalAddress { get; set; }
    }

    public partial class WebApiApplication
    {
        public void OnAppStart()
        {
            Task.Run(async () =>
            {
                using (var context = new BrokerDatabase())
                {
                    var checkAndWake = await context.BrokerUsers.ToListAsync();
                    var checkAndWake2 = await context.BrokerStrategies.Take(1).ToListAsync();

                    //ErrorFx.Log("test");

                }

            });
 
        }


    }



}