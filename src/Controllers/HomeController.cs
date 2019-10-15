using Piggy;
using PigSwitch.Hubs;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Bitmex;
using Twilio.TwiML.Voice;

namespace PigSwitch.Controllers
{
    /// <summary>
    /// Moved to Azure Functions 
    /// </summary>
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            //ErrorFx.V($"Home Controller  // Build: {GeneralFunctions.GetBuildIdentifier()}");
            var s  = GeneralFunctions.GetBuildIdentifier();
            return Json(s, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> Index4()
        {
            using (var context = new BrokerDatabase())
            {
                var sql = $"SELECT TOP 1 * FROM BrokerStrategies";
                var s  = await context.BrokerStrategies.SqlQuery(sql).FirstOrDefaultAsync();
                return Json(s, JsonRequestBehavior.AllowGet);
            }            
        }

        public async Task<ActionResult> Index3()
        {
            var clientId = "7qXqe_z8SwJMOQaQhoQ6cgoh";
            var clientKey = "uzqk-ZfD326E82y2ELyhZhzwbYtzQe5RiRRFae9dDz9W9I_z";
            clientId = "t2jwhMCUpJogACxN6KQl54UY";
            clientKey = "krxuuo1cHU8DhHSrtuFm0SyecoQ1SJVxBnLIYlUU7baattzW";
                
            var client = new BitmexHttpClient(clientId, clientKey, false);

            var br = new BrokerRequest();
            br.ClientId = clientId;
            br.ClientKey = clientKey;
            br.LiveNet = false;
            br.Symbol = "XBTUSD";
            br.Quantity = 1;
            br.Price = 19233.3m;

            br.Side = "Buy";
            br.Instructions = "ParticipateDoNotInitiate";            
            br.OrderType = "Limit";

            var resp = await client.PostOrdersAsync(br);

            var pos_resp = await client.GetPositionsAsync();
            var pos = pos_resp.Result?.FirstOrDefault();


            //if (pos?.isOpen == true)
            //{
            //    br.OrderType = "Stop";
            //    br.Side = pos.Side == "Buy" ? "Sell" : "Buy";
            //    br.StopPrice = pos.liquidationPrice + 5;
            //    var stop = await client.PostOrdersAsync(br);
            //    return Json(new { resp, pos, stop }, JsonRequestBehavior.AllowGet);
            //}
            
            //Submit stop loss at market on testnet 
            //test response 
            
            return Json(new { resp, pos }, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> Index2()
        {
            var clientId = "7qXqe_z8SwJMOQaQhoQ6cgoh";
            var clientKey = "uzqk-ZfD326E82y2ELyhZhzwbYtzQe5RiRRFae9dDz9W9I_z";

            var client = new BitmexHttpClient(clientId, clientKey, true);
            var positions = await client.GetWalletSummaryAsync();
            var position = positions?.Result?.FirstOrDefault();
            var test = position.amount * 0.00000001m * 6234;
            //var positionSide = position?.currentQty > 0 ? "Buy" : "Sell";
            //var hasOpenPosition = position?.isOpen == true && positionSide == "Buy";

            return Json(positions, JsonRequestBehavior.AllowGet);
        }
        public ActionResult poink(int i, string t, string s)
        {
            var hub = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<AppHub>();
            
            using (var context = new BrokerDatabase())
            {
                if (t == "ox" && i > 0)
                {
                    var obj = context.BrokerStrategyTrades.FirstOrDefault(a => a.ID == i);
                    if (obj != null)
                    {
                        var user = context.BrokerUsers.FirstOrDefault(a => a.ID == obj.FK_UserID);
                        if (!string.IsNullOrWhiteSpace(user?.SignalR))
                        {
                            AppHub.NotifyTrade(obj, user.SignalR, user.Email);                            
                        }
                    }
                }
            }

            return Json(new { Success = true, Request.RawUrl }, JsonRequestBehavior.AllowGet);
        }

    }
}
