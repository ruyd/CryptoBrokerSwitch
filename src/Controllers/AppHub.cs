using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Piggy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace PigSwitch.Hubs
{
    public partial class AppHub : Hub
    {
        private static readonly ConcurrentDictionary<Guid?, UserServerModel> ActiveModels = new ConcurrentDictionary<Guid?, UserServerModel>();
        private static readonly ConcurrentDictionary<string, Guid> ActiveConnections = new ConcurrentDictionary<string, Guid>();

        public override async Task OnConnected()
        {
            await OnConn();
            await base.OnConnected();
        }
        public override async Task OnReconnected()
        {
            await OnConn();
            await base.OnReconnected();
        }

        private async Task OnConn()
        {
            string connId = Context.ConnectionId;

            string ipAddress = Context.Request.Environment.ContainsKey("server.RemoteIpAddress") ? Context.Request.Environment["server.RemoteIpAddress"]?.ToString() : "N/A";
            string chromeId = Context.Request.QueryString["tm"];
            string chromeEmail = Context.Request.QueryString["u"];

            using (BrokerDatabase context = new BrokerDatabase())
            {
                BrokerUser user = await context.BrokerUsers.FirstOrDefaultAsync(a => a.Email == chromeEmail);

                if (user == null)
                {
                    user = new BrokerUser();
                    user.ID = Guid.NewGuid();
                    user.ChromeId = chromeId;
                    user.Email = chromeEmail;
                    user.IPAddress = ipAddress;
                    user.SignalR = connId;

                    user.DateTimeCreated = DateTime.UtcNow;
                    user.DateTimeConnected = DateTime.UtcNow;
                    user.DateTimeUpdated = DateTime.UtcNow;
                                        
                    context.BrokerUsers.Add(user);

                    //Start Workflow to Welcome New User and Email Invitation                     
                    Task.Run(() => StartWelcomeFlow(user));
                }

                if (user != null)
                {
                    //update                       
                    user.IPAddress = ipAddress;
                    user.DateTimeConnected = DateTime.UtcNow;
                    user.DateTimeUpdated = DateTime.UtcNow;
                    user.DateTimeDisconnected = null;
                    user.ChromeId = chromeId;
                    user.SignalR = connId;

                    await context.SaveChangesAsync();

                    var prefs = await context.BrokerPreferences.FirstOrDefaultAsync(a => a.FK_UserID == user.ID);
                    if (prefs == null)
                    {
                        //prefs = new BrokerPreference();
                        //prefs.
                        //context.BrokerPreferences.Add(prefs);

                    }

                    OnUserConnected(user, prefs);
                }
            }

            await SendModel();
            SendInstruments();
        }

        private void StartWelcomeFlow(BrokerUser user)
        {
            //Nofy Me 
            OinkNotifications.SendPushNofy($"New User: {user.Email} ID: {user.ChromeId}");

            //Email Welcome 


        }

        public void Ping()
        {
            //Clients.Caller.Pong();
            Clients.Client(Context.ConnectionId).Pong();
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            Guid userId;
            ActiveConnections.TryRemove(Context.ConnectionId, out userId);

            string connId = Context.ConnectionId;
            using (BrokerDatabase context = new BrokerDatabase())
            {
                BrokerUser user = await context.BrokerUsers.FirstOrDefaultAsync(a => a.SignalR == connId);
                if (user != null)
                {
                    user.SignalR = null;
                    user.DateTimeDisconnected = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    RemoveUserFromPrivateStream(user.ID);
                }
            }

            await base.OnDisconnected(stopCalled);
        }

        [HubMethodName("l")]
        public void Login(ChromeIn data)
        {

            Clients.Client(Context.ConnectionId).rl("aja");

        }

        [HubMethodName("y")]
        public void VerifyKey(dynamic command)
        {
            string net = command.Net;
            Guid? userId = command.Token;

            Task.Run(async () =>
            {
                using (BrokerDatabase context = new BrokerDatabase())
                {
                    BrokerUser user = await context.BrokerUsers.FirstAsync(a => a.ID == userId);

                    string key = net != "TestNet" ? user.LiveKey : user.TestKey;
                    string id = net != "TestNet" ? user.LiveID : user.TestID;

                    OrderResponse response = null;
                    try
                    {
                        BrokerRequest req = new BrokerRequest(id, key, "XBTUSD", 20000, 1, "Buy", "KEY TEST", "Limit",
                            "ParticipateDoNotInitiate");

                        req.LiveNet = net != "TestNet";
                        req.IsKeyTest = true;

                        response = await PiggyBroker.SendBitmexAsync(req);
                    }
                    catch (Exception ex)
                    {
                        response = OrderResponse.Error(ex.Message, 404);
                        ex.SaveToDB();
                    }

                    if (!string.IsNullOrWhiteSpace(response.orderID))
                    {

                        Clients.Client(user.SignalR).nm($"Test Trade: PASSED", "bounce", true);
                        //send demo sucess to finish wiring trades table biatch =) 

                        /* 
                         * PKID
                         * Trade#
                         * LongShort
                         * Price
                         * Size
                         * Slip %
                         * Status ordStatus
                         * Message
                         * Success

                         */

                        Clients.Caller.rt(new
                        {
                            TradeNum = "T",
                            Signal = net,
                            Price = 20000,
                            Qty = 1,
                            Slip = .05m,
                            Status = response.ordStatus,
                            Message = response.ordRejReason,
                            Success = true,
                            Time = DateTimeOffset.Now
                        });

                    }
                    else
                    {
                        Clients.Caller.nm($"Test Trade: FAILED<br><small>{response.error?.message}</small>", "error", true);
                    }
                }
            });
        }

        [HubMethodName("x")]
        public void BrokerControl(dynamic command)
        {
            int stateId = command.State;
            Guid? userId = command.Token;
            int? tabId = command.TabId;
            int? startNum = command.Start ?? 1;

            decimal? profitPer = command.TV?.profitPercent;
            decimal? profitFactor = command.TV?.profitFactor;
            int? backtestLen = command.TV?.backtestLen;

            Task.Run(async () =>
            {
                try
                {

                    using (BrokerDatabase context = new BrokerDatabase())
                    {
                        BrokerUser user = await context.BrokerUsers.FirstAsync(a => a.ID == userId);
                        BrokerPreference prefs = await context.BrokerPreferences.FirstOrDefaultAsync(a => a.FK_UserID == userId) ?? new BrokerPreference();

                        //Assign
                        BrokerStrategy s = prefs.ToStrategy();
                        s.StatusID = stateId;

                        if (command.TV != null)
                        {
                            s.StrategyName = command.TV.strategyName;
                            s.StrategyId = command.TV.strategyId;

                            s.CandleInterval = command.TV.candle;
                            s.ChartId = command.TV.chart;
                            s.ChromeTabId = tabId;

                            string sym = command.TV.symbol;
                            string[] split = sym.Split(':');
                            s.Exchange = split.FirstOrDefault();
                            s.Symbol = split.LastOrDefault();
                        }

                        ////
                        BrokerStrategy existing = await context.BrokerStrategies.FirstOrDefaultAsync(a => a.FK_UserID == userId && a.StrategyId == s.StrategyId);
                        if (existing == null)
                        {
                            existing = s;
                            context.BrokerStrategies.Add(s);
                        }
                        else
                        {
                            s.ID = existing.ID;
                        }

                        //will update s and e
                        existing.DateTimeUpdated = DateTime.UtcNow;
                        existing.StatusID = stateId;
                        existing.ChromeTabId = tabId;
                        context.SaveChanges();


                        //Testing this bit 
                        BrokerStrategiesRun run = s.ToRun();

                        BrokerStrategiesRun currentRun = await context.BrokerStrategyRuns.Where(a => a.FK_StrategyID == s.ID).OrderByDescending(a => a.ID).FirstOrDefaultAsync();
                        if (currentRun == null)
                        {
                            currentRun = s.ToRun();
                        }
                        else
                        {
                            //LastRun was stopped, start a new one just because... testing 
                            if (currentRun.DateTimeStopped != null)
                            {
                                currentRun = run;
                                context.BrokerStrategyRuns.Add(run);

                                //get pk
                                await context.SaveChangesAsync();
                            }
                            else
                            {
                                //Re-Use 
                                //update properties and reset 
                            }
                        }

                        currentRun.StartStrategyName = s.StrategyName;
                        currentRun.StartChartId = s.ChartId;
                        currentRun.StartInterval = s.CandleInterval;
                        currentRun.StartQuantity = s.Quantity;
                        currentRun.BacktestCount = backtestLen;
                        currentRun.EstimatedProfitPercent = profitPer;
                        currentRun.EstimatedProfitRatio = profitFactor;
                        currentRun.StartTradeNum = startNum;

                        if (stateId == 1)
                        {
                            currentRun.DateTimePaused = null;

                            //brokerStart(command);
                            // hub.Clients.All.NewMessage("que es la que start/resume", "bounce");
                        }

                        else if (stateId == 2)
                        {
                            currentRun.DateTimePaused = DateTime.UtcNow;
                            //pause 
                            //  hub.Clients.All.NewMessage("que es la que pause", "error");

                        }
                        else if (stateId == 3)
                        {
                            currentRun.DateTimeStopped = DateTime.UtcNow;
                            //stop
                            Clients.All.nm("Stop?", "panic", true);
                        }

                        //
                        user.StatusID = stateId;
                        user.DateTimeUpdated = DateTime.UtcNow;

                        //DONT OVERWRITE
                        if (currentRun != null)
                        {
                            user.LastRunID = currentRun.ID;
                        }

                        if (currentRun != null)
                        {
                            user.LastStrategyID = currentRun.FK_StrategyID;
                        }

                        await context.SaveChangesAsync();
                    }

                }
                catch (Exception ex)
                {
                    ex.SaveToDB();
                    Console.WriteLine("BrokerEx::", ex);
                }
            });
        }


        /// <summary>
        /// TODO:
        /// -- Add Binance
        /// -- Add Unsupported Exchange Message 
        /// 
        /// </summary>
        /// <param name="workAlways"></param>
        /// <returns>Async Various</returns>  
        [HubMethodName("bo")]
        public void BrokerOrder(dynamic workAlways)
        {
            dynamic json = Newtonsoft.Json.JsonConvert.SerializeObject(workAlways);

            Task.Run(async () =>
            {
                try
                {
                    //vomit - catch any specific deser errors                        
                    NewOrderPost post = Newtonsoft.Json.JsonConvert.DeserializeObject<NewOrderPost>(json);
                    BrokerStrategiesTrade result = await BrokerPost(post);
                    //post.IPAddress = Context.Request.Environment["server.RemoteIpAddress"]?.ToString();
                    if (result != null)
                    {
                        NotifyTrade(result, Context.ConnectionId, post.ChromeEmail);
                    }
                }
                catch (Exception ex)
                {
                    ErrorFx.SaveToDB(ex, json);
                    Console.WriteLine("BrokerEx::", ex);
                }
            });
        }

        public static void NotifyTrade(BrokerStrategiesTrade newTrade, string connId, string email, bool close = false)
        {
            Task.Run(async () =>
            {
                IHubContext hub = GlobalHost.ConnectionManager.GetHubContext<AppHub>();

                //Single trade doesnt have previous trade profit, going bazooka approach 
                //hub.Clients.Client(connId).ReceiveTrade(newTrade.ToJ());

                var r = await GetModelAsync(email);
                hub.Clients.Client(connId).rm(r);

                string text = $"{newTrade.TradeNum}# New {newTrade.EntrySignal?.ToUpper()}: {newTrade.EntryQuantity}@{newTrade.EntryPrice} Slip: {newTrade.EntrySplippage?.ToString("p")} S: {newTrade.EntryMessage ?? newTrade.ExchangeError}";

                if (close)
                {
                    text = $"{newTrade.EntrySignal.ToCamelCase()} position #{newTrade.TradeNum} Closed as {newTrade.ExitType} Qty: {newTrade.EntryQuantity.toString("n")} | {newTrade.EntryPrice.toString("n")} => {newTrade.ExitPrice.toString("n")} Diff: {newTrade?.ExitDiff.toString("n")} Hope: {newTrade?.ProfitPercent.toString("n")} Fees: {newTrade.FeesUSD.toString("c")} Profit: {newTrade?.ProfitUSD.toString("c")}";
                }

                //move messages to client side 
                if (newTrade.Success == true)
                {
                    hub.Clients.Client(connId).nm(text, newTrade.ExitProfitPercent > 0 ? "yes" : "kick", true);
                }
                else if (newTrade.StatusID == 5)
                {
                    hub.Clients.Client(connId).nm("New Order (Not!): Broker Off", "fomo", true);
                }
                else
                {
                    hub.Clients.Client(connId).nm(newTrade.EntryMessage, "error", true);
                }

            });

        }

        /// <summary>
        /// rename to orders 
        /// </summary>
        /// <returns></returns>
        [HubMethodName("a")]
        public async Task ArchiveTrades()
        {
            ErrorFx.V("Archiving Closed Trades...", 2);
            using (BrokerDatabase context = new BrokerDatabase())
            {
                string ipAddress = Context.Request.Environment["server.RemoteIpAddress"]?.ToString();
                string chromeId = Context.Request.QueryString["tm"];
                string chromeEmail = Context.Request.QueryString["u"];

                //result 
                ChromeExtModel r = new ChromeExtModel();

                string sql = $"UPDATE t SET Archive = 1 FROM BrokerStrategiesTrades t INNER JOIN BrokerUsers u ON t.FK_UserID = u.ID WHERE u.Email = '{chromeEmail}' AND t.StatusID != 1";
                int changeCount = await context.Database.ExecuteSqlCommandAsync(sql);
                await SendModel();
            }
        }

        [HubMethodName("sm")]
        public async Task SendModel()
        {
            string ipAddress = Context.Request.Environment["server.RemoteIpAddress"]?.ToString();
            string chromeId = Context.Request.QueryString["tm"];
            string chromeEmail = Context.Request.QueryString["u"];
            var r = await GetModelAsync(chromeEmail);
            Clients.Caller.rm(r);
        }

        private static async Task<ChromeExtModel> GetModelAsync(string email)
        {
            //result 
            ChromeExtModel r = new ChromeExtModel();
            using (BrokerDatabase context = new BrokerDatabase())
            {
                BrokerUser user = await context.BrokerUsers.FirstOrDefaultAsync(a => a.Email == email);
                if (user == null)
                {
                    r.Message = "MANT";
                }
                else
                {
                    r.ID = user.ID;
                    r.Authed = user.Enabled;
                    r.Existing = true;

                    //rework 
                    r.Settings = user.ToSettings();
                    r.Preferences = await context.BrokerPreferences.FirstOrDefaultAsync(a => a.FK_UserID == user.ID);
                    r.Enabled = user.Enabled;

                    //BrokerStrategiesRun run = await context.BrokerStrategyRuns.FirstOrDefaultAsync(a => a.ID == user.LastRunID);
                    string sql = $"SELECT TOP 30 * FROM BrokerStrategiesTrades(NOLOCK) WHERE FK_UserID = '{user.ID}' AND Archive IS NULL ORDER BY ID DESC";
                    List<BrokerStrategiesTrade> tradeList = await context.Database.SqlQuery<BrokerStrategiesTrade>(sql).ToListAsync();
                    r.BrokeredTrades = tradeList.ToList();

                }
            }
            return r;
        }
    }

}