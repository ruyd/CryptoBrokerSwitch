using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Piggy;
using Bitmex;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;
using Microsoft.AspNet.SignalR.Hubs;

namespace PigSwitch.Hubs
{
    public partial class AppHub
    {
        private const string SIG_ENDPOINT = "/realtime";
        private const string MXENDPOINT = "/realtimemd?transport=websocket&b64=1";

        private static readonly ConcurrentDictionary<Guid, string> AddedToPrivate = new ConcurrentDictionary<Guid, string>();
        private static WebSocket BitmexPrivateSocket;
        private static WebSocket BitmexPublicSocket;

        private static DateTime? PrivateStreamLastInitializedOn;
        private static DateTime? PrivateStreamClosedOn;
        private static DateTime? PublicStreamLastInitializedOn;
        private static DateTime? PublicStreamClosedOn;
        private static bool Bitinit = false;

        private static readonly BitmexHttpClient WalletHttp = new BitmexHttpClient();
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly BitmexHttpClient BitmexHttp = new BitmexHttpClient() { LiveTurnedOn = true };

        //private static BitmexInstrument _LastXbt;
        //private static BitmexInstrument LastXbt
        //{
        //    get { if (_LastXbt == null) _LastXbt = new BitmexInstrument() { symbol = "XBTUSD" }; return _LastXbt; }
        //    set { _LastXbt = value; }
        //}

        private static BitmexInstrument SocketXbt => BitmexInstruments.FirstOrDefault(a => a.symbol == "XBTUSD");

        private static List<BitmexInstrument> _BitmexInstruments;
        private static List<BitmexInstrument> BitmexInstruments
        {
            get { if (_BitmexInstruments == null) _BitmexInstruments = new List<BitmexInstrument>() { new BitmexInstrument() { symbol = "XBTUSD" } }; return _BitmexInstruments; }
            set { _BitmexInstruments = value; }
        }

        private static List<BitmexOrderBook> LastBook;
        private static DateTime? lastFundingTime;

        private static System.Timers.Timer _HealthTimer;

        private void OnUserConnected(BrokerUser user, BrokerPreference prefs)
        {
            var exist = ActiveConnections.FirstOrDefault(a => a.Value == user.ID);

            ActiveConnections.TryAdd(Context.ConnectionId, user.ID);

            var m = UserServerModel.FromUser(user, prefs);

            ActiveModels.TryAdd(user.ID, m);

            if (Bitinit && BitmexPrivateSocket?.State == WebSocketState.Open)
            {
                AddUserToPrivateStream(m);
            }

            //Getting Wallet                       
            Task.Run(async () =>
            {
                var br = new BrokerRequest();
                br.LiveNet = user.LiveTurnedOn == true;
                br.ClientId = m.LiveTurnedOn ? user.LiveID : user.TestID;
                br.ClientKey = m.LiveTurnedOn ? user.LiveKey : user.TestKey;
                m.WalletBalance = await GetWalletAsync(br);
                SendWallet(m);
            });

            if (Bitinit)
                return;

            Task.Run(async () =>
            {
                using (var context = new BrokerDatabase())
                {
                    var openTrades = await context.BrokerStrategyTrades.Where(a => a.FK_UserID == user.ID && a.StatusID == 1).ToListAsync();
                    if (openTrades != null)
                    {
                        foreach (var item in openTrades)
                        {
                            ErrorFx.V($"Adding Open Trade to Buffer #{item.TradeNum}", 1);
                            m.TradeBuffer.Add(item);
                        }
                    }
                }
            });


            InitializeBitmex();
        }

        private void InitializeBitmex()
        {
            ErrorFx.Level = 2;
            ErrorFx.V($"Initializing Greed Engine // Build: {GeneralFunctions.GetBuildIdentifier()}");

            try
            {

                if (_HealthTimer == null)
                {
                    _HealthTimer = new System.Timers.Timer();
                    _HealthTimer.Interval = 30 * 1000;
                    _HealthTimer.Elapsed += _HealthTimer_Elapsed;
                    _HealthTimer.AutoReset = true;
                }

                _HealthTimer.Start();

                Task.Run(() => { InitializePrivateStream(); });

                Task.Run(() => { InitializePublicStream(); });

            }
            catch (Exception ex)
            {
                ex.SaveToDB("Initialize Bitmex 1d04");
            }

            Bitinit = true;
        }

        private void _HealthTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Check Public WebSocket and Reconnect - Do not bind reconnect to events motherfucker 
            //if (BitmexPublicSocket == null || BitmexPublicSocket.State != WebSocketState.Open ||
            //    BitmexPublicSocket.State != WebSocketState.Connecting)
            //{
            //    InitializePublicStream();
            //}

            Task.Run(GreedTimerRun);
        }

        private void InitializePublicStream()
        {
            PublicStreamLastInitializedOn = DateTime.UtcNow;
            ErrorFx.V("Initializing Public Stream", 3);

            BitmexPublicSocket = new WebSocket("wss://www.bitmex.com/realtime");

            BitmexPublicSocket.AutoSendPingInterval = 30; //30 seconds ping
            BitmexPublicSocket.EnableAutoSendPing = true;

            BitmexPublicSocket.MessageReceived += ws_MessageReceived;

            BitmexPublicSocket.Error += (x, y) =>
            {
                y.Exception.SaveToDB("PublicStream::+Error");
            };

            BitmexPublicSocket.Closed += (x, y) =>
            {
                ErrorFx.V("PublicStream::+Closed", 1);
                PublicStreamClosedOn = DateTime.UtcNow;
            };

            BitmexPublicSocket.DataReceived += (x, y) => { };

            BitmexPublicSocket.Opened += (x, y) =>
            {
                ErrorFx.V("PublicStream::+Opened", 1);
                PublicStreamClosedOn = null;
            };

            try
            {
                BitmexPublicSocket.Open();
            }
            catch (Exception ex)
            {
                ex.SaveToDB("InitializePublicStream");
            }
        }

        private void AddUserToPrivateStream(UserServerModel user)
        {
            if (AddedToPrivate.ContainsKey(user.ID))
            {
                ErrorFx.V($"Already Added {user.Email} to private", 3);
                return;
            }
            else
            {
                ErrorFx.V($"Adding {user.Email} to private", 3);
            }

            var cmd = $"[1,\"{user.ID}\",\"priv\"]";
            BitmexPrivateSocket.Send(cmd);

            var expires = DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeSeconds();
            string sig = user.GetSig(expires);

            cmd =
                $"[0,\"{user.ID}\",\"priv\",{{\"op\":\"authKeyExpires\",\"args\":[\"{user.KeyId}\",{expires},\"{sig}\"]}}]";
            BitmexPrivateSocket.Send(cmd);

            //,\"position\",\"order\",\"wallet\"
            cmd = $"[0,\"{user.ID}\",\"priv\",{{\"op\":\"subscribe\",\"args\":[\"execution\",\"position\"]}}]";
            BitmexPrivateSocket.Send(cmd);

            if (AddedToPrivate.TryAdd(user.ID, "stub"))
            {
                ErrorFx.V($"Added {user.Email}", 1);
            }
            else
            {
                ErrorFx.V($"Failed Add {user.Email} to private", 3);
            }


        }

        private void RemoveUserFromPrivateStream(Guid userId)
        {
            var cmd = $"[2,\"{userId}\",\"priv\"]";
            BitmexPrivateSocket.Send(cmd);

            string meh;
            AddedToPrivate.TryRemove(userId, out meh);
        }

        private void ActivatePrivateStreams()
        {
            foreach (var model in ActiveModels)
            {
                AddUserToPrivateStream(model.Value);
            }
        }


        private void InitializePrivateStream()
        {
            PrivateStreamLastInitializedOn = DateTime.UtcNow;
            ErrorFx.V("Initializing Private Stream", 3);

            AddedToPrivate.Clear();

            BitmexPrivateSocket = new WebSocket("wss://www.bitmex.com/realtimemd?transport=websocket&b64=1");

            BitmexPrivateSocket.AutoSendPingInterval = 30; //5 seconds ping
            BitmexPrivateSocket.EnableAutoSendPing = true;

            BitmexPrivateSocket.MessageReceived += ws_PrivateMessageReceived;
            BitmexPrivateSocket.Error += (x, y) =>
            {
                y.Exception.SaveToDB("PrivateStream::+Error");
            };

            BitmexPrivateSocket.Closed += (x, y) =>
            {
                ErrorFx.V("PrivateStream::+Closed", 1);
                PrivateStreamClosedOn = DateTime.UtcNow;
            };

            BitmexPrivateSocket.DataReceived += (x, y) => { };

            BitmexPrivateSocket.Opened += (x, y) =>
            {
                ErrorFx.V("PrivateStream::+Opened", 1);
                ActivatePrivateStreams();
                PrivateStreamClosedOn = null;
            };

            try
            {
                BitmexPrivateSocket.Open();
            }
            catch (Exception ex)
            {
                ex.SaveToDB("Opening Socket");
            }
        }

        private void ws_PrivateMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message == null) return;

            if (e.Message.Contains("Welcome"))
            {
                ErrorFx.V($"PrivateSocket:Welcome", 1, e.Message);
            }
            else if (e.Message.Contains("{"))
            {
                int payStart = e.Message?.IndexOf("{") ?? 0;
                string userPrefix = e.Message.Substring(3, payStart - 3);
                int userEnd = userPrefix?.IndexOf(",") ?? 0;
                string possibleId = userPrefix.Substring(1, userEnd - 2);
                int payEnd = e.Message.Length - payStart - 1;
                string payload = e.Message.Substring(payStart, payEnd);

                Guid userId;
                if (!Guid.TryParse(possibleId, out userId))
                {
                    return;
                }

                var json = JsonConvert.DeserializeObject<JObject>(payload);
                if (json["table"] != null)
                {
                    var table = json["table"]?.Value<string>();
                    if (table == "position")
                    {
                        var message = json.ToObject<BitmexStreamMessage<BitmexPosition>>();
                        ErrorFx.V($"PrivateSocket:Position: {message.action} | {userId}", 3, e.Message);
                        if (message.First == null) return;

                        //////////////// 
                        //"action":"partial", {"account":595181,"symbol":"XBTUSD","currency":"XBt","underlying":"XBT","quoteCurrency":"USD","commission":0.00075,"initMarginReq":0.04,"maintMarginReq":0.005,"riskLimit":20000000000,"leverage":25,"crossMargin":false,"deleveragePercentile":1,"rebalancedPnl":95522,"prevRealisedPnl":-11651,"prevUnrealisedPnl":0,"prevClosePrice":6707.79,"openingTimestamp":"2018-08-27T20:00:00.000Z","openingQty":0,"openingCost":-35000,"openingComm":127159,"openOrderBuyQty":0,"openOrderBuyCost":0,"openOrderBuyPremium":0,"openOrderSellQty":0,"openOrderSellCost":0,"openOrderSellPremium":0,"execBuyQty":1000,"execBuyCost":14877000,"execSellQty":1500,"execSellCost":22300500,"execQty":-500,"execCost":7423500,"execComm":20439,"currentTimestamp":"2018-08-27T21:50:00.401Z","currentQty":-500,"currentCost":7388500,"currentComm":147598,"realisedCost":-46500,"unrealisedCost":7435000,"grossOpenCost":0,"grossOpenPremium":0,"grossExecCost":7433500,"isOpen":true,"markPrice":6726.8,"markValue":7433000,"riskValue":7433000,"homeNotional":-0.07433,"foreignNotional":500,"posState":"","posCost":7435000,"posCost2":7435000,"posCross":0,"posInit":297400,"posComm":5800,"posLoss":0,"posMargin":303200,"posMaint":42975,"posAllowance":0,"taxableMargin":0,"initMargin":0,"maintMargin":301200,"sessionMargin":0,"targetExcessMargin":0,"varMargin":0,"realisedGrossPnl":46500,"realisedTax":0,"realisedPnl":-101098,"unrealisedGrossPnl":-2000,"longBankrupt":0,"shortBankrupt":0,"taxBase":35000,"indicativeTaxRate":0,"indicativeTax":0,"unrealisedTax":0,"unrealisedPnl":-2000,"unrealisedPnlPcnt":-0.0003,"unrealisedRoePcnt":-0.0067,"simpleQty":-0.0744,"simpleCost":-500,"simpleValue":-500,"simplePnl":0,"simplePnlPcnt":0,"avgCostPrice":6725,"avgEntryPrice":6725,"breakEvenPrice":6719.5,"marginCallPrice":6968.5,"liquidationPrice":6968.5,"bankruptPrice":7004.5,"timestamp":"2018-08-27T21:50:00.401Z","lastPrice":6726.8,"lastValue":7433000}]}]
                        //["update","data":[{"account":595181,"symbol":"XBTUSD","currency":"XBt","currentTimestamp":"2018-08-27T21:53:20.058Z","markPrice":6724.76,"timestamp":"2018-08-27T21:53:20.058Z","lastPrice":6724.76,"currentQty":-500,"simpleQty":-0.0744,"liquidationPrice":6968.5}]}]
                        //["update","data":[{"account":595181,"symbol":"XBTUSD","currency":"XBt","currentTimestamp":"2018-08-27T21:53:25.060Z","markPrice":6725.56,"markValue":7434500,"riskValue":7434500,"homeNotional":-0.074345,"maintMargin":302700,"unrealisedGrossPnl":-500,"unrealisedPnl":-500,"unrealisedPnlPcnt":-0.0001,"unrealisedRoePcnt":-0.0017,"timestamp":"2018-08-27T21:53:25.060Z","lastPrice":6725.56,"lastValue":7434500,"currentQty":-500,"simpleQty":-0.0744,"liquidationPrice":6968.5}]}]
                        //markPrice":6726.04,"markValue":7434000,"riskValue":7434000,"homeNotional":-0.07434,"maintMargin":302200,"unrealisedGrossPnl":-1000,"unrealisedPnl":-1000,"unrealisedPnlPcnt":-0.0001,"unrealisedRoePcnt":-0.0034,"timestamp":"2018-08-27T21:55:30.245Z","lastPrice":6726.04,"lastValue":7434000,"currentQty":-500,"simpleQty":-0.0744,"liquidationPrice":6968.5}]}]
                        //markPrice":6725.87,"timestamp":"2018-08-27T21:55:35.065Z","lastPrice":6725.87,"currentQty":-500,"simpleQty":-0.0744,"liquidationPrice":6968.5}]}]

                        UserServerModel model;
                        ActiveModels.TryGetValue(userId, out model);
                        if (model != null)
                        {
                            model.PrivateConnected = true;

                            foreach (var stream in message.data)
                            {
                                var p = model.Positions.FirstOrDefault(a => a.symbol == stream.symbol);
                                if (p == null)
                                {
                                    //don't care if it's a delta
                                    p = stream;
                                    model.Positions.Add(p);
                                }

                                if (stream.currentQty != null)
                                {
                                    p.currentQty = stream.currentQty;
                                }

                                if (stream.unrealisedPnl != null)
                                {
                                    p.unrealisedPnl = stream.unrealisedPnl;
                                }

                                if (stream.unrealisedPnlPcnt != null)
                                {
                                    p.unrealisedPnlPcnt = stream.unrealisedPnlPcnt;
                                }

                                if (stream.unrealisedCost != null)
                                {
                                    p.unrealisedCost = stream.unrealisedCost;
                                }

                                if (stream.markPrice != null)
                                {
                                    p.markPrice = stream.markPrice;
                                }

                                if (stream.lastPrice != null)
                                {
                                    p.lastPrice = stream.lastPrice;
                                }

                                if (stream.breakEvenPrice != null)
                                {
                                    p.breakEvenPrice = stream.breakEvenPrice;
                                }

                                if (stream.unrealisedRoePcnt != null)
                                {
                                    p.unrealisedRoePcnt = stream.unrealisedRoePcnt;
                                }

                                if (stream.unrealisedCost != null)
                                {
                                    p.unrealisedCost = stream.unrealisedCost;
                                }

                                p.timestamp = DateTime.UtcNow;
                                p.isOpen = p.currentQty > 0 || p.currentQty < 0;

                                if (stream.unrealisedPnlPcnt != null)
                                {
                                    SendPosition(userId, p);
                                }
                            }
                        }

                        ErrorFx.V($"PrivateSocket:Position: {message.First.Side} ({message.First.currentQty.toString()}) PNL: {message.First.unrealisedPnl.toString()}  | {userId}", 4, e.Message);

                        ///////////
                        // GREED 
                        /////////

                        //Change leverage to cross if its really good? 

                    }
                    else if (table == "wallet")
                    {
                        ErrorFx.V($"PrivateSocket:Wallet: {userId}", 2, e.Message);
                        var exec = json.ToObject<BitmexStreamMessage<BitmexWallet>>();
                        if (exec.First != null)
                        {
                            ErrorFx.V($"Wallet: {exec.First.account} {exec.First.amount} {exec.First.currency}", 1, e.Message);
                        }
                    }
                    else if (table == "order")
                    {
                        ErrorFx.V($"PrivateSocket:Order: {userId}", 2, e.Message);
                        var exec = json.ToObject<BitmexStreamMessage<OrderResponse>>();
                        if (exec.First != null)
                        {
                            ErrorFx.V($"Order: {exec.First.ordType} {exec.First.ordStatus} {exec.First.side} {exec.First.price} {exec.First.stopPx}", 1, e.Message);
                        }
                    }
                    else if (table == "execution")
                    {
                        ErrorFx.V($"PrivateSocket:Execution: {userId}", 4, e.Message);
                        var exec = json.ToObject<BitmexStreamMessage<OrderResponse>>();

                        if (exec.First != null)
                        {
                            ErrorFx.V($"From Bitmex <= {exec.First.ordType} {exec.First.ordStatus} {exec.First.side} {exec.First.price} | {exec.First.text}", 1, e.Message);

                            if (exec.First.ordStatus == "Filled" || exec.First.ordStatus == "PartiallyFilled")
                            {
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await OnOrderFilledAsync(userId, exec.First);
                                    }
                                    catch (Exception ex)
                                    {
                                        ex.SaveToDB(e.Message);
                                    }
                                });
                            }
                        }
                    }
                }
            }
        }

        private void ws_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ErrorFx.V($"PublicSocket:Verbose:", 5, e.Message);

            if (e.Message.Contains("Welcome"))
            {
                ErrorFx.V($"PublicSocket:Welcome", 1, e.Message);

                Task.Run(async () =>
                {
                    if (BitmexInstruments.Count <= 1)
                    {
                        ErrorFx.V($"PublicSocket:Instruments Get", 1, e.Message);
                        var url = "https://www.bitmex.com/api/v1/instrument/active";
                        BitmexInstruments = await BitmexHttp.GetInstrumentsAsync();
                    }

                    var items = string.Join("\",\"", BitmexInstruments.Select(a => string.Concat("instrument:", a.symbol)).ToArray());
                    string subscription = $"{{\"op\":\"subscribe\",\"args\":[\"{items}\"]}}";
                    ErrorFx.V("Send subscription request: {0}", 1, subscription);
                    BitmexPublicSocket.Send(subscription);

                });

            }
            else if (e.Message.Contains("{"))
            {
                var json = JsonConvert.DeserializeObject<JObject>(e.Message);
                if (json["table"] != null)
                {
                    var table = json["table"]?.Value<string>();
                    if (table == "instrument")
                    {
                        try
                        {
                            var message = json.ToObject<BitmexStreamMessage<BitmexInstrument>>();

                            ErrorFx.V($"Instrument: {message.action}", 5, e.Message);

                            if (message.First == null) return;

                            foreach (var msg in message.data)
                            {
                                var inx = BitmexInstruments.FirstOrDefault(a => a.symbol == msg.symbol);
                                if (inx == null || message.action == BitmexAction.Partial)
                                {
                                    if (inx == null)
                                        ErrorFx.V($"Add", 5, e.Message);

                                    if (message.action == BitmexAction.Partial)
                                    {
                                        ErrorFx.V($"Full Instrument Set {msg.symbol}: {msg.lastPrice}", 1, e.Message);
                                        if (inx != null)
                                            BitmexInstruments.Remove(inx);
                                    }

                                    inx = msg;
                                    BitmexInstruments.Add(msg);
                                }

                                if (message.action == BitmexAction.Update)
                                {
                                    inx.timestamp = message.First.timestamp ?? DateTime.UtcNow;

                                    //{"table":"instrument","action":"update","data":[{"symbol":"XBTUSD","lastPrice":6716,"lastTickDirection":"MinusTick","lastChangePcnt":0.0062,"timestamp":"2018-08-27T20:59:10.488Z"}]}
                                    if (message.First.lastPrice > 0)
                                    {
                                        inx.lastPrice = message.First.lastPrice;
                                    }

                                    if (!string.IsNullOrWhiteSpace(message.First.lastTickDirection))
                                    {
                                        inx.lastTickDirection = message.First.lastTickDirection;
                                    }

                                    if (message.First.lastChangePcnt != null)
                                    {
                                        inx.lastChangePcnt = message.First.lastChangePcnt;
                                    }

                                    //{"table":"instrument","action":"update","data":[{"symbol":"XBTUSD","openValue":10124237242380,"fairPrice":6715.98,"markPrice":6715.98,"timestamp":"2018-08-27T20:59:10.000Z"}]}
                                    if (message.First.markPrice > 0)
                                    {
                                        inx.markPrice = message.First.markPrice;
                                    }

                                    //{"table":"instrument","action":"update","data":[{"symbol":"XBTUSD","lastPriceProtected":6715.39,"timestamp":"2018-08-27T20:59:10.000Z"}]}
                                    if (message.First.lastPriceProtected != null)
                                    {
                                        inx.lastPriceProtected = message.First.lastPriceProtected;
                                    }

                                    //{ "table":"instrument","action":"update","data":[{"symbol":"XBTUSD","indicativeSettlePrice":6715.39,"timestamp":"2018-08-27T20:59:10.000Z"}]}
                                    //if (message.First.indicativeSettlePrice > 0) {   }

                                    //{ "table":"instrument","action":"update","data":[{"symbol":"XBTUSD","totalVolume":756980719637,"volume":52591035,"totalTurnover":10197458280306688,"turnover":782237215054,"openInterest":679935342,"openValue":10123557307038,"timestamp":"2018-08-27T20:59:07.088Z"}]}
                                    if (message.First.totalVolume > 0)
                                    {
                                        inx.volume = message.First.volume;
                                        inx.totalVolume = message.First.totalVolume;
                                        //SocketXbt.totalTurnover = message.First.totalTurnover;
                                        //SocketXbt.turnover = message.First.turnover;
                                    }

                                    //{ "table":"instrument","action":"update","data":[{"symbol":"XBTUSD","prevPrice24h":6674.5,"lastChangePcnt":0.0062,"timestamp":"2018-08-27T20:59:00.000Z"}]}
                                    if (message.First.prevPrice24h > 0)
                                    {
                                        inx.prevPrice24h = message.First.prevPrice24h;
                                    }

                                    //{"table":"instrument","action":"update","data":[{"symbol":"XBTUSD","volume24h":2077144020,"turnover24h":30976355102779,"homeNotional24h":309763.55102779286,"foreignNotional24h":2077144020,"timestamp":"2018-08-27T20:59:00.000Z"}]}
                                    if (message.First.volume24h > 0)
                                    {
                                        inx.volume24h = message.First.volume24h;
                                        //SocketXbt.turnover24h = message.First.turnover24h;
                                        //SocketXbt.homeNotional24h = message.First.homeNotional24h;
                                        //SocketXbt.foreignNotional24h = message.First.homeNotional24h;                                    
                                    }

                                    if (message.First.highPrice > 0)
                                    {
                                        inx.highPrice = message.First.highPrice;
                                    }

                                    if (message.First.midPrice > 0)
                                    {
                                        inx.midPrice = message.First.midPrice;
                                    }

                                    if (message.First.lowPrice > 0)
                                    {
                                        inx.lowPrice = message.First.lowPrice;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorFx.V($"Instrument Stream Error: {ex.Message}", 0, ex.StackTrace);
                        }
                    }
                    else if (table == "orderBookL2")
                    {
                        //ErrorFx.Info($"PublicSocket:Execution: {userId}", e.Message, null, "Greed");                        
                    }
                }
            }
        }

        private void SendInstruments(bool toAll = false)
        {
            var result = BitmexInstruments.Select(a => new
            {
                a.symbol,
                a.lastPrice,
                a.lastChangePcnt,
                a.volume,
                a.volume24h,
                a.markPrice,
                a.highPrice,
                a.lowPrice,
                a.lastTickDirection,
                a.prevClosePrice
            }).OrderBy(a => a.symbol).ToList();

            if (toAll)
                Clients.All.rc(result);
            else
                Clients.Caller.rc(result);
        }

        private async Task GreedTimerRun()
        {
            ///////////////
            //Price Action Live Vars
            ////////////// 
            //LastXbt = await BitmexHttp.GetInstrumentAsync("XBT");
            if (SocketXbt?.markPrice > 0)
            {
                //At freaking .5 ticks this is not cool 
                LastBook = await BitmexHttp.GetBookAsync("XBT", 40);
                if (LastBook?.Count > 0)
                {
                    SocketXbt.BuySize = LastBook.Where(a => a.side == "Buy").Sum(a => a.size);
                    SocketXbt.SellSize = LastBook.Where(a => a.side == "Sell").Sum(a => a.size);
                }

                // await SaveInstrumentAsync(LastXbt);
                
                SendInstruments(toAll:true);
            }

            //////////////
            //Public Socket Health Check 
            if (Bitinit && BitmexPublicSocket.State != WebSocketState.Open && BitmexPublicSocket.State != WebSocketState.Connecting)
            {
                ErrorFx.V($"Public Socket Health Timer", 3);
                if ((DateTime.UtcNow - PublicStreamClosedOn)?.TotalSeconds > 30 && (DateTime.UtcNow - PublicStreamLastInitializedOn)?.TotalSeconds > 60)
                {
                    ErrorFx.V($"Reviving Public Socket", 0);
                    InitializePublicStream();
                }
            }

            //////////////
            //Private Socket Health Check 
            if (Bitinit && BitmexPrivateSocket.State != WebSocketState.Open && BitmexPrivateSocket.State != WebSocketState.Connecting)
            {
                ErrorFx.V($"Private Socket Health Timer", 3);
                if ((DateTime.UtcNow - PrivateStreamClosedOn)?.TotalSeconds > 30 && (DateTime.UtcNow - PrivateStreamLastInitializedOn)?.TotalSeconds > 60)
                {
                    ErrorFx.V($"Reviving Private Socket", 0);
                    InitializePrivateStream();
                }
            }

            //Users Loop

            using (var context = new BrokerDatabase())
            {
                var br = new BrokerRequest();

                //reuse client!
                foreach (var model in ActiveModels)
                {
                    br.ClientId = model.Value.KeyId;
                    br.ClientKey = model.Value.KeySecret;
                    br.LiveNet = model.Value.LiveTurnedOn;

                    //new code baby sit 
                    try
                    {
                        //////////////////
                        //Wallet Summary 
                        ErrorFx.V($"Getting Wallet Summary: {model.Value.Email}", 4);
                        model.Value.WalletBalance = await GetWalletAsync(br);
                        SendWallet(model.Value);
                    }
                    catch (Exception ex)
                    {
                        ErrorFx.V($"Wallet Summary Error: {ex.Message}", 0, ex.StackTrace);
                    }

                    //////////////////
                    //Position Orders 
                    var open = model.Value.Positions.Where(a => a.isOpen == true).ToList();
                    if (open.Count > 0
                            && (model.Value.LastFilledOn == null
                                || (DateTimeOffset.UtcNow - model.Value.LastFilledOn)?.TotalSeconds > 30))
                    {
                        BitmexHttpClient client = new BitmexHttpClient(model.Value.KeyId, model.Value.KeySecret, model.Value.LiveTurnedOn);
                        foreach (var item in open)
                        {
                            var trade = model.Value.TradeBuffer.LastOrDefault(a => a.Symbol == item.symbol && a.StatusID == 1);
                            if (trade == null) continue;

                            //Only on fails for now
                            if (trade.StopPrice == 0)
                            {
                                await AddStopsAsync(model.Value, item, trade);
                            }

                            if (trade.TakePrice == 0)
                            {
                                await AddTakesAsync(model.Value, item, trade);
                            }



                            //ErrorFx.V($"Checkin Stops for {item.symbol}: {item.Side}: {item.currentQty}", 3);
                            //var orders = await client.GetOrdersAsync(item.symbol, true);
                            //if (orders.Count(a => a.stopPx != null) == 0)
                            //{
                            //    //ErrorFx.V($"Adding Missing Stops and Takes", 1);

                            //    //if (trade == null)
                            //    //{
                            //    //    trade = new BrokerStrategiesTrade();
                            //    //    trade.AskQuantity = item.PositiveQuantity;
                            //    //    trade.EntryPrice = item.avgEntryPrice;
                            //    //}

                        }
                    }

                    //Broadcast open trades and Leverage 

                    //1. Are stops present? 
                    //2. What is the roe? alert? modify?
                    //3. Any open limit orders really close to filling but not quite? ammend 

                    ///////////////
                    //Auto Close
                    ////////////// 
                    if ((DateTime.UtcNow - lastFundingTime)?.TotalMinutes < 475)
                        return;

                    //3 minutes prior close out 
                    if ((DateTime.UtcNow.Hour == 3 ||
                         DateTime.UtcNow.Hour == 11 ||
                         DateTime.UtcNow.Hour == 19) && DateTime.UtcNow.Minute > 57)
                    {
                        //funding rate esta negativo y estoy en un long y a favor, 
                        //if (rate < 0 && !long 
                        var badSide = SocketXbt?.fundingRate < 0 ? "Sell" : "Buy";
                        ErrorFx.V($"Pre-Funding Check: {model.Value.Email} Bad Side: {badSide} | Rate: {SocketXbt?.fundingRate}", 1);

                        //IF fundingRate negative, Short Pay Longs, if positive, Long Pays Shorts, so close                        
                        //if we are in a bad or good trade then??? check pnl
                        var pos = model.Value.Positions.FirstOrDefault();
                        if (pos?.Side == badSide && (pos?.currentQty > 0 || pos?.currentQty < 0)
                            && model.Value.Preferences?.EnableAutoClose == true)
                        {
                            br.Symbol = pos.symbol;
                            await ClosePositionAsync(br, pos, true, model.Value.TradeBuffer.LastOrDefault(a => a.Symbol == pos.symbol && a.StatusID == 1));
                        }

                        lastFundingTime = DateTime.UtcNow;
                    }
                }
            }
        }

        [HubMethodName("mt")]
        public async Task ManualTrade(ManualTradeData data)
        {
            ErrorFx.V($"Check Position", 1, data.Serialize());

            Guid userId;
            var hasId = ActiveConnections.TryGetValue(Context.ConnectionId, out userId);
            var model = ActiveModels.FirstOrDefault(a => a.Key == userId);
            if (hasId && model.Value != null)
            {
                var br = new BrokerRequest(model.Value.KeyId, model.Value.KeySecret, model.Value.LiveTurnedOn);
                br.Symbol = data.Instrument;
                br.Quantity = data.Quantity ?? model.Value.Preferences.Quantity;
                br.Side = data.IsLong ? "Buy" : "Sell";
                br.TryLogic = model.Value.Preferences.LimitOptionId;
                br.Price = data.Price ?? SocketXbt.markPrice ?? -1;
                br.Comment = $"MANUAL";
                br.LiveNet = model.Value.LiveTurnedOn;
                br.GroupById = data.Instrument;
                br.ExchangeId = 2;
                br.OrderType = model.Value.Preferences?.EnableMarket == true ? "Market" : "Limit";
                br.TryLogic = model.Value.Preferences?.LimitOptionId;

                //Positions is hot, do not overstay welcome
                var position = model.Value.Positions.FirstOrDefault(a => a.symbol == data.Instrument);
                if (position.isOpen == true && position.Side == "Buy" && !data.IsLong)
                {
                    //Mismatched Side, a close? 
                }

                if (br.Price > 0)
                {
                    ErrorFx.V($"Manual => {br.Side}: {br.Quantity} at {br.Price} as {br.OrderType}", 1);
                    var resp = await PiggyBroker.SendBitmexAsync(br);

                    var maxTries = 10;
                    int i = 1;
                    while (resp.Overloaded)
                    {
                        if (i > maxTries)
                        {
                            ErrorFx.V($"Bitmex Overloaded and I give up!", 0);
                            break;
                        }

                        await Task.Delay(500);

                        ErrorFx.V($"Bitmex Overloaded: Retry {i}", 0);
                        br.NeedsReset = false;
                        resp = await PiggyBroker.SendBitmexAsync(br);
                        i++;
                    }

                    //invert close/true param
                    if (resp.Success)
                    {
                        Clients.Caller.nm("Trade Sent!", "cheer", true);
                    }
                    else
                    {
                        Clients.Caller.nm("Trade Failed!", "error", true);
                    }
                }
                else
                {
                    Clients.Caller.nm("Price?", "kick", true);
                }
            }
        }


        /// <summary>
        /// Refactor into ConvertToPeg()
        /// </summary>
        /// <returns></returns>
        [HubMethodName("cp")]
        public async Task CheckPosition()
        {
            try
            {
                ErrorFx.V($"Check Position", 3);

                //If Position Open but No open Orders then 
                Guid userId;
                var hasId = ActiveConnections.TryGetValue(Context.ConnectionId, out userId);
                var model = ActiveModels.FirstOrDefault(a => a.Key == userId);
                if (model.Value == null)
                {
                    ErrorFx.V($"No Model for {Context.ConnectionId} | {userId}", 1);
                    Clients.Caller.nm($"no Model", "error", true);
                }
                else
                {
                    //Positions is hot, do not overstay welcome
                    var open = model.Value.Positions.Where(a => a.isOpen == true).ToList();
                    if (open.Count == 0)
                    {
                        ErrorFx.V($"No Positions", 3, model.Value.Positions.Serialize());
                        Clients.Caller.nm("No Position to Check", "slap", true);
                        return;
                    }
                    else
                    {
                        ErrorFx.V($"Open Positions: {open.Count}", 3, open.Serialize());
                    }

                    BitmexHttpClient client = new BitmexHttpClient(model.Value.KeyId, model.Value.KeySecret, model.Value.LiveTurnedOn);

                    using (var context = new BrokerDatabase())
                    {
                        foreach (var item in open)
                        {
                            ErrorFx.V($"Getting Orders for {item.symbol}: {item.Side}: {item.currentQty}", 1);
                            var orders = await client.GetOrdersAsync(item.symbol, true);

                            if (orders.Count(a => a.stopPx != null) == 0)
                            {
                                ErrorFx.V($"Adding Missing Stops and Takes", 1);

                                var trade = model.Value.TradeBuffer.LastOrDefault(a => a.Symbol == item.symbol && a.StatusID == 1);

                                if (trade == null)
                                {
                                    trade = await context.BrokerStrategyTrades.Where(a =>
                                        a.FK_UserID == userId
                                        && a.StatusID == 1
                                        && a.Symbol == item.symbol
                                        && a.EntrySignal == item.Signal)
                                    .OrderByDescending(a => a.ID)
                                    .FirstOrDefaultAsync();
                                }

                                if (trade == null)
                                {
                                    ErrorFx.V($"Creating Fake Trade for Stops", 1);
                                    trade = new BrokerStrategiesTrade();
                                    trade.AskQuantity = item.PositiveQuantity;
                                    trade.EntryPrice = item.avgEntryPrice;
                                }

                                //trade.StatusID != 2
                                if (trade.EntryMessage != "Nibbled")
                                {
                                }

                                //await AddStopsAsync(model.Value, item, trade);

                                //Take Profit 
                                if (model.Value.Preferences.EnableTake == true)
                                {
                                    if (SocketXbt.lastPrice > trade.EntryPrice)
                                    {
                                        //Push stop up!
                                        trade.EntryPrice = SocketXbt.lastPrice;
                                    }

                                    await AddTakesAsync(model.Value, item, trade);
                                }

                                Clients.Client(model.Value.SignalR).nm("Adding Missing Stops and Takes", "uff", true);

                            }
                            else
                            {
                                ErrorFx.V($"Position Okay", 3);
                                Clients.Client(model.Value.SignalR).nm($"Position has {orders.Count} stops!", "uff", true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.SaveToDB($"Failed to Check Position {ex.Message}");
            }
        }

        private async Task<BitmexWalletSummary> GetWalletAsync(BrokerRequest br)
        {
            //BitmexHttpClient client = new BitmexHttpClient(br.ClientId, br.ClientKey, br.LiveNet);
            WalletHttp.Set(br.ClientId, br.ClientKey, br.LiveNet);
            ErrorFx.V($"Getting Wallet for {br.ClientId} live:{br.LiveNet}", 4);
            var response = await WalletHttp.GetWalletSummaryAsync();
            return response.Result?.LastOrDefault();//transactType = "Total"
        }

        private void SendWallet(UserServerModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.SignalR) && model.WalletBalance != null)
            {
                Clients.Client(model.SignalR).rb(new
                {
                    model.WalletBalance.amount,
                    model.WalletBalance.unrealisedPnl,
                    model.WalletBalance.realisedPnl,
                    model.WalletBalance.walletBalance,
                    model.WalletBalance.marginBalance,
                    balanceUSD = model.WalletBalance.amount * 0.00000001m * SocketXbt.lastPrice 
                });
            }
        }

        private async Task SaveInstrumentAsync(BitmexInstrument obj)
        {
            using (var context = new BrokerDatabase())
            {
                obj.DateTimeCreated = DateTime.UtcNow;
                context.BitmexInstruments.Add(obj);
                try
                {
                    await context.SaveChangesAsync();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        ErrorFx.Log($"Entity of type {eve.Entry.Entity.GetType().Name} in state {eve.Entry.State} has the following validation errors:");
                        foreach (var ve in eve.ValidationErrors)
                        {
                            ErrorFx.Log($"- Property: {ve.PropertyName}, {ve.ErrorMessage}");
                        }
                    }
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                {
                    ErrorFx.Log(ex.GetBaseException().Message, ex.GetBaseException().StackTrace);
                }
            }
        }

        /// <summary>
        /// TODO: Change to Bulk Endpoint 
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        private async Task<BrokerStrategiesTrade> BrokerPost(NewOrderPost post)
        {
            using (BrokerDatabase context = new BrokerDatabase())
            {
                try
                {
                    ErrorFx.V("POST::NEW ORDER", 1, post.Serialize());

                    //Get User - Get from ServerModel
                    Guid? userId = post.ID;
                    UserServerModel model;
                    ActiveModels.TryGetValue(userId, out model);
                    if (model == null)
                    {
                        ErrorFx.V($"YEAH NO MODEL - WTF {userId} Died {ActiveConnections.Serialize()}", 0, ActiveModels.Serialize());
                        //Recreate model so vago, take a nap now
                        var user = await context.BrokerUsers.FirstAsync(a => a.ID == userId);
                        var prefs = await context.BrokerPreferences.FirstAsync(a => a.FK_UserID == userId);
                        model = UserServerModel.FromUser(user, prefs);
                        ActiveModels.TryAdd(userId, model);

                        ErrorFx.V($"Revived? {userId} {ActiveConnections.Serialize()}", 1, ActiveModels.Serialize());
                        //return null;
                    }

                    ErrorFx.V($"NewOrder:BufferDump: {model.TradeBuffer.Count} ", 3, model.TradeBuffer.Select(a => new { a.TradeNum, a.ID, a.UniqueID, a.DateTimeCreated, a.EntrySide }).Serialize());

                    string rawSym = post.Data.symbol;
                    string[] sym = rawSym.Split(':');
                    string exchange = sym.FirstOrDefault();
                    string symbol = sym.LastOrDefault();
                    string strategyName = post.Data.strategyName;
                    string strategyTvId = post.Data.strategyId;

                    StringBuilder nofyText = new StringBuilder();
                    if (post.Data.orders?.Count > 0)
                    {
                        RawOrder prevTradeEntry = post.Data.orders?[2];
                        RawOrder prevTradeExit = post.Data.orders?[1];
                        RawOrder newTradeEntry = post.Data.orders?[0];

                        nofyText.Append($"{post.Data.strategyName}:{symbol} | ");

                        //if (symbol == "BTCUSDT")
                        //{
                        //    symbol = "XBTUSD";
                        //    exchange = "BITMEX";
                        //    newTradeEntry.Price = SocketXbt.lastPrice ?? LastXbt?.lastPrice;
                        //}

                        BrokerStrategiesTrade newTrade = post.ToTrade(newTradeEntry);
                        newTrade.FK_UserID = post.ID;
                        newTrade.Symbol = symbol;

                        newTrade.SourceName = "SWITCH";

                        newTrade.TradeNum = newTradeEntry.TradeNum;
                        newTrade.StatusID = -1;
                        newTrade.AskQuantity = model.Preferences?.Quantity ?? 0; //que explote si no hay 
                        newTrade.AskType = model.Preferences?.EnableMarket == true ? "Market" : "Limit";

                        context.BrokerStrategyTrades.Add(newTrade);

                        //Let's start comparing too detect too quick an entry when price immediately corrects 
                        newTrade.LastPrice = SocketXbt.lastPrice ?? -1;
                        newTrade.MarkPrice = SocketXbt.markPrice ?? -1;

                        newTrade.BuySize = LastBook?.Where(a => a.side == "Buy").Sum(a => a.size);
                        newTrade.SellSize = LastBook?.Where(a => a.side == "Sell").Sum(a => a.size);
                        newTrade.Balance = model.WalletBalance?.walletBalance * 0.00000001m;

                        ////////
                        // Buffer
                        var lastTrade = model.TradeBuffer.LastOrDefault(a => a.Symbol == symbol);
                        if (lastTrade != null)
                        {
                            //Missed trade alert
                            int? tradeDiff = lastTrade.TradeNum - newTrade.TradeNum;
                            if (tradeDiff > 1)
                            {
                                string s = $"{(newTradeEntry.Signal == lastTrade.EntrySignal ? "Missed Side!" : "Sequence Broken")}: New #:{newTradeEntry.TradeNum} {newTradeEntry.Signal} Prev#:{lastTrade.TradeNum} {lastTrade.EntrySignal}";
                                ErrorFx.V(s);
                                OinkNotifications.SendPushNofy(s);
                            }

                            ErrorFx.V($"Preceeding Trade from Buffer: {lastTrade.TradeNum}  S:{lastTrade.StatusID} On {lastTrade.DateTimeCreated} ");
                            //other thread?
                            try
                            {
                                model.TradeBuffer.Remove(lastTrade);
                            }
                            catch (Exception ex)
                            {
                                ErrorFx.V($"Error removing last trade from buffer", 0, ex.StackTrace);
                            }
                        }

                        model.TradeBuffer.Add(newTrade);

                        // ***************************
                        // BEGIN SEND 

                        OrderResponse resp = null;

                        BrokerRequest br = new BrokerRequest();

                        br.UniqueID = newTrade.UniqueID;
                        br.ClientId = model.KeyId;
                        br.ClientKey = model.KeySecret;
                        br.Symbol = symbol;
                        br.Price = newTrade.AskPrice;
                        br.Quantity = newTrade.AskQuantity;
                        br.Side = newTrade.EntrySide;
                        br.Comment = $"{newTrade.UniqueID}";
                        br.LiveNet = model.LiveTurnedOn;
                        br.GroupById = symbol;
                        br.ExchangeId = PiggyBroker.GetExchangeId(exchange);
                        br.OrderType = model.Preferences?.EnableMarket == true ? "Market" : "Limit";
                        br.TryLogic = model.Preferences?.LimitOptionId;

                        //Toggle Off 
                        string blockError = $"{newTrade.EntrySide} Off";
                        bool allowSendToBroker = newTrade.EntryLong && model.Preferences?.EnableLong == true
                                                 || newTrade.EntryShort && model.Preferences?.EnableShort == true;

                        //Exchange not ready block
                        if (br.ExchangeId != 1 && br.ExchangeId != 2)
                        {
                            allowSendToBroker = false;
                            blockError = $"{exchange} Off";
                        }

                        //////// NUDGE SPEED EVEN HIGHER 
                        ///THINK: CHANGE TO BULK ENDPOINT, SEND LIST WITH CLOSE AS MARKET ALONG WITH NEW TRADE IF OPEN (LIST COUNT: 2)

                        /////////////////////////////// 
                        //Get Position
                        OrderResponse closeResponse = null;
                        var position = model.Positions.FirstOrDefault(a => a.symbol == newTrade.Symbol);
                        if (position != null)
                        {
                            ErrorFx.V($"Got Position from Memory {position.isOpen} Sym: {position.symbol} Q: {position.currentQty} Age: {position.AgeInSeconds}", 1);
                        }

                        //Trusting Socket More, now only get if 
                        if (position == null
                            || position.currentQty == null
                            || !model.PrivateConnected
                            || BitmexPrivateSocket?.State != WebSocketState.Open)
                        {
                            if (position != null)
                            {
                                ErrorFx.V($"Socket Problems, Getting Http Position: State: {BitmexPrivateSocket?.State}", 0);
                            }

                            var positions = await PiggyBroker.GetPositionsAsync(br);
                            position = positions?.FirstOrDefault(a => a.symbol == newTrade.Symbol);
                            ErrorFx.V($"Got Fresh Position {position?.isOpen}, Sym: {position?.symbol} Q: {position?.currentQty} Age: {position?.AgeInSeconds}", 0);
                        }

                        if (position != null)
                        {
                            br.HasOpenPosition = position.isOpen == true;

                            ErrorFx.V($"POSITION {position.isOpen}: {position.Side} ({position.currentQty}) Entry: {newTrade.EntryPrice} B/E: {position.breakEvenPrice} DIFF: {newTrade.EntryPrice - position.breakEvenPrice}", 1, position.Serialize());

                            //Check Position and Sides                    
                            if (position.isOpen == true)
                            {
                                if (position.currentQty != null)
                                {
                                    if (position.Side == br.Side)
                                    {
                                        if (model.Preferences?.EnableSame != true)
                                        {
                                            ErrorFx.Log("Same Side - Blocked");
                                            allowSendToBroker = false;
                                            blockError = "Same Side";
                                        }
                                        else
                                        {
                                            if (position.PositiveQuantity + newTrade.AskQuantity >= model.Preferences.MaxSame)
                                            {
                                                allowSendToBroker = false;
                                                blockError = "Max Qty";
                                            }
                                        }
                                    }

                                    if (position.Side != br.Side 
                                            && (position.Side == "Buy" && model.Preferences.EnableLong == true || 
                                                position.Side == "Sell" && model.Preferences.EnableShort == true))
                                    {
                                        closeResponse = await ClosePositionAsync(br, position, false, lastTrade);
                                    }
                                }
                            }
                            else
                            {
                                ErrorFx.V($"Position Is Not Open", 3);
                            }
                        }
                        else
                        {
                            ErrorFx.V($"Position Is Null", 0);
                        }

                        if (!br.HasOpenPosition)
                        {
                            ErrorFx.V($"NO POSITION FOUND BEFORE NEW TRADE", 3);
                        }

                        newTrade.CancelledPrevious = lastTrade?.StatusID == 0;

                        //LUBE PRICE 
                        if (model.Preferences.EnableGel == true)
                        {
                            var diff = model.Preferences.GelBy ?? 1;
                            var suggested = br.Price + (br.Side == "Buy" ? diff : -diff);
                            ErrorFx.V($"Greed Gel:: Ask: {br.Price} Sug: {suggested} ", 1);
                            suggested = Convert.ToDecimal(Math.Round((double)(suggested * 2)) / 2);
                            br.Price = suggested;
                            newTrade.AskPrice = br.Price;//Factor in Slip and Profit Calculations bitch
                        }

                        /////////////////////////////////
                        //SEND TO BITMEX 

                        //Toggle Off block 
                        resp = allowSendToBroker
                            ? await PiggyBroker.SendBitmexAsync(br)
                            : OrderResponse.Error(blockError, 5, newTrade);

                        var maxTries = 30;
                        int i = 1;
                        while (resp.Overloaded)
                        {
                            if (i > maxTries)
                            {
                                ErrorFx.V($"Bitmex's Overloaded and I give up!", 0);
                                break;
                            }

                            if (newTrade.StatusID > -1)
                            {
                                //Got accepted during loop, even thou return overloaded
                                //break, work done
                            }

                            await Task.Delay(1000);

                            if (i > 7)
                            {
                                await Task.Delay(1000);
                            }

                            ErrorFx.V($"Bitmex Overloaded: Retry {i}", 0);
                            br.NeedsReset = false;
                            resp = await PiggyBroker.SendBitmexAsync(br);
                            i++;
                        }

                        newTrade.EntryMessage = resp.ordStatus;
                        newTrade.Success = resp.Success;
                        newTrade.RequestCode = br.Serialize().Clean(1500);
                        newTrade.ResponseCode = resp.Serialize().Clean(1500);

                        if (resp.Success)
                        {
                            newTrade.EntryPrice = resp.price;//sometimes its fucking wrong by 1
                            newTrade.EntryQuantity = resp.orderQty;
                            newTrade.EntryType = resp.ordType;
                            newTrade.EntryTime = DateTime.UtcNow;

                            newTrade.ExchangeOrderID = resp.orderID;

                            if (resp.ordStatus == "Filled")
                            {
                                newTrade.StatusID = 1;
                            }
                            else if (resp.ordStatus == "New")
                            {
                                newTrade.StatusID = 0;
                            }
                            else if (resp.ordStatus == "Cancelled")
                            {
                                newTrade.StatusID = 86;
                            }
                            else if (resp.ordStatus == "PartiallyFilled")
                            {
                                newTrade.StatusID = 1;
                            }
                            else
                            {
                                newTrade.StatusID = 9;
                                ErrorFx.Log("Switch: Status not coded...", resp.ordStatus, resp.Serialize());
                            }
                        }
                        else
                        {
                            //gotta be a few responses 
                            newTrade.ExchangeError = resp.error?.message;
                            newTrade.StatusID = resp.error?.code ?? 500;

                            //If we are allowed but got an error then...
                            if (allowSendToBroker)
                            {
                                OinkNotifications.SendPushNofy($"Error: {newTrade.EntryMessage} {resp.error?.message}");
                            }

                            ErrorFx.V($"SWITCH: Order Failed {resp.error.Serialize()}", 0, resp.Serialize());
                        }

                        //Quick Hacks - bad string methods =( memory mess
                        newTrade.Sanitize();

                        await context.SaveChangesAsync();

                        context.Entry(newTrade).State = EntityState.Modified;
                        //Context.Entry(newTrade).State = EntityState.Detached;

                        ErrorFx.V($"Saved: {newTrade.ID}", 2);

                        return newTrade;
                    }
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException e)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (System.Data.Entity.Validation.DbEntityValidationResult eve in e.EntityValidationErrors)
                    {
                        sb.Append($"Error: {eve.Entry.Entity.GetType().Name} s: {eve.Entry.State} ");
                        foreach (System.Data.Entity.Validation.DbValidationError ve in eve.ValidationErrors)
                        {
                            sb.Append($"- Property: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                        }
                    }
                    e.SaveToDB(sb.ToString());
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                {
                    ex.InnerException?.SaveToDB();
                }
                catch (Exception ex)
                {
                    ex.SaveToDB();
                    ex.InnerException?.SaveToDB();
                    Console.WriteLine("BrokerEx::", ex);
                }
                return null;
            }
        }

        private async Task<OrderResponse> ClosePositionAsync(BrokerRequest br, BitmexPosition position,
            bool autoClose = false, BrokerStrategiesTrade trade = null)
        {
            OrderResponse close = null;
            ErrorFx.V($"Closing Position: {position.Side} {position.currentQty} ", 1);
            br.HasOpenPosition = position.isOpen == true;
            if (position.isOpen == true)
            {
                if (position.currentQty != null)
                {
                    BrokerRequest cr = br.New();

                    //Super Important - Should be preceeding trade not newTrade
                    //or use model.OpenTrade 

                    if (trade != null)
                    {
                        cr.Comment = $"{trade.UniqueID}";
                    }

                    cr.IsClose = true;
                    cr.TimeInForce = "FillOrKill";
                    //cr.Comment = autoClose ? "Pre-Funding " : "" + $"Force Close Limit => Take: {(position.unrealisedPnl * .00000001m * position.lastPrice).toString("c")}";
                    cr.Quantity = position.PositiveQuantity; // orderQty is not specified, a 'Close' order has an orderQty equal to your current position's size

                    close = await PiggyBroker.SendBitmexAsync(cr);
                    ErrorFx.V(autoClose ? "Pre-Funding " : "" + $"Limit Close: {close.ordStatus ?? close.ordRejReason}", 1, close.Serialize());

                    if (!close.Success)
                    {
                        cr.OrderType = "Market";
                        cr.TimeInForce = null;
                        //cr.Comment = autoClose ? "Pre-Funding " : "" + $"Force Close Market {(position.unrealisedPnl * .00000001m * position.lastPrice)}";
                        cr.Quantity = position.PositiveQuantity;
                        close = await PiggyBroker.SendBitmexAsync(cr);
                        ErrorFx.V(autoClose ? "Pre-Funding " : "" + $"Force Market Close: {close.ordStatus ?? close.ordRejReason}", 1, close.Serialize());
                    }
                }
            }
            return close;
        }

        /// <summary>
        /// Posibles 
        /// 1. Regular NoPositionBefore - New Position Fill, add stops and takes 
        /// 2. Existing Position Close, and remove stop and takes 
        /// 3. Remember, no unfilled trades pass here. Position Closes are stop, take, manual from bmex.com 
        /// </summary>
        private async Task OnOrderFilledAsync(Guid userId, OrderResponse resp)
        {
            try
            {
                ErrorFx.V($"OnFilled::{resp.ordType} {resp.side} S:{resp.ordStatus} P:{resp.price} ({resp.orderQty}) | {resp.text}", 1, resp.Serialize());

                if (resp.text == "Funding")
                {
                    OinkNotifications.SendPushNofy($"Funding: {resp.commission}");
                    ErrorFx.V($"OnFilled::Funding Commission: {resp.commission}", 1);
                    return;
                }

                UserServerModel model;
                ActiveModels.TryGetValue(userId, out model);

                //////// STOP LOSS / TAKE PROFIT
                using (var context = new BrokerDatabase())
                {
                    if (model == null)
                    {
                        ErrorFx.V($"OnOrderFilled::User null WTF ", 1);
                        return;
                    }

                    if (model.Preferences == null)
                    {
                        ErrorFx.V($"OnOrderFilled::{resp.orderID} Prefs empty WTF ", 1);
                        return;
                    }

                    model.LastFilledOn = DateTimeOffset.UtcNow;

                    var br = new BrokerRequest();
                    br.Symbol = resp.symbol;
                    br.LiveNet = model.LiveTurnedOn;
                    br.ClientId = model.KeyId;
                    br.ClientKey = model.KeySecret;//change to encryptor and try it out

                    ///////
                    // Buffer  

                    var mp = model.Positions.FirstOrDefault(a => a.symbol == resp.symbol);
                    ErrorFx.V($"Memory Position: {mp.isOpen} {mp.currentQty} Age: {mp.AgeInSeconds} ", 1);

                    ErrorFx.V($"Comment Parse: {resp.text?.Split('\n').LastOrDefault()}", 0);

                    Guid uniqueId;
                    var hasUniqueId = Guid.TryParse(resp.text?.Split('\n').LastOrDefault(), out uniqueId);
                    if (!hasUniqueId)
                    {
                        hasUniqueId = Guid.TryParse(resp.clOrdID, out uniqueId);
                    }

                    if (hasUniqueId)
                    {
                        ErrorFx.V("Doing weird text thing you wanted", 0, resp.text);
                        var split = resp.text.Split('\n').ToList();
                        split.Remove(split.LastOrDefault());
                        resp.text = string.Join(" ", split.ToArray());

                        //ErrorFx.V(resp.text, 0, resp.text.Substring(0, resp.text.IndexOf('\n')));
                    }

                    ErrorFx.V($"Filled:BufferDump: {model.TradeBuffer.Count} Looking for ID: {uniqueId} ", 3, model.TradeBuffer.Select(a => new { a.TradeNum, a.ID, a.StatusID, a.UniqueID, a.DateTimeCreated, a.EntrySide }).Serialize());

                    var fullClose = resp.IsClose && (mp == null || resp.orderQty >= mp.PositiveQuantity);
                    ErrorFx.V($"Full Close: {fullClose}", 0);

                    var databaseList = new List<BrokerStrategiesTrade>();
                    var trade = model.TradeBuffer.FirstOrDefault(a => a.UniqueID == uniqueId && uniqueId != Guid.Empty);
                    if (trade != null)
                    {
                        ErrorFx.V($"Got trade from Buffer: {trade?.TradeNum} ID: {trade?.ID} Status: {trade?.StatusID} {trade?.EntryType} {trade?.UniqueID} On {trade.DateTimeCreated} ", 1);

                        //If position updates too fast then recheck
                        fullClose = resp.orderQty >= trade.AskQuantity;

                        if (fullClose)
                        {
                            ErrorFx.V($"Removing from Buffer {trade.TradeNum} since its a close", 1);
                            model.TradeBuffer.Remove(trade);
                        }
                    }
                    else
                    {
                        //NULL
                        //Get List of last 10 trades 
                        var sql = $"SELECT TOP 5 * FROM BrokerStrategiesTrades(NOLOCK) WHERE FK_UserID = '{userId}' AND Symbol = '{resp.symbol}' " +
                            $" AND StatusID IN (0, 1, 2) ORDER BY ID DESC";

                        //Tracking but not attached
                        databaseList = await context.BrokerStrategyTrades.SqlQuery(sql).ToListAsync();
                        ErrorFx.V($"Database List Count: {databaseList.Count}", 1, databaseList.Select(a => new { a.TradeNum, a.ID, a.StatusID, a.UniqueID, a.DateTimeCreated, a.EntrySide }).Serialize());

                        //Not happy
                        trade = databaseList.FirstOrDefault(a => a.UniqueID == uniqueId ||
                                    a.Symbol == resp.symbol
                                        && ((resp.IsClose && a.EntrySide != resp.side && a.StatusID == 1) || !resp.IsClose && a.StatusID == 0));

                        //WARNING: Machete 
                        if (trade == null && databaseList.Count == 1)
                        {
                            ErrorFx.V($"MACHETE", 0);
                            trade = databaseList.First();
                        }

                        ErrorFx.V($"Got trade from DB: {trade?.TradeNum} ID: {trade?.ID} Status: {trade?.StatusID} {trade?.EntryType} {trade?.UniqueID} On {trade?.DateTimeCreated} vs {resp.timestamp} ", 1);

                        //Machete
                        if (trade != null)
                        {
                            //If position updates too fast then recheck
                            fullClose = resp.orderQty >= trade.AskQuantity;

                            var bufferCheck = model.TradeBuffer.FirstOrDefault(a => a.UniqueID == trade.UniqueID);
                            if (bufferCheck != null)
                            {
                                if (bufferCheck.ID != trade.ID)
                                {
                                    ErrorFx.V($"Aquiiiiiiiiiiii! {bufferCheck.ID} {bufferCheck.TradeNum} vs {trade.ID} {trade.TradeNum}", 0);
                                }

                                if (fullClose)
                                {
                                    ErrorFx.V($"Removing from Buffer ID: {bufferCheck.ID} {bufferCheck.TradeNum} since its a close", 1);
                                    model.TradeBuffer.Remove(bufferCheck);
                                }
                            }
                        }
                    }

                    if (trade != null)
                    {
                        ErrorFx.V($"Attaching: {trade.TradeNum} ID: {trade?.ID}  Status: {trade?.StatusID} {trade.EntryType} {trade.UniqueID} ", 1);
                        context.BrokerStrategyTrades.Attach(trade);
                    }
                    else
                    {
                        //Manual Bitmex baby
                        trade = resp.ToEntryTrade();
                        trade.FK_UserID = userId;
                        trade.LastPrice = SocketXbt?.lastPrice;
                        trade.MarkPrice = SocketXbt?.markPrice;

                        context.BrokerStrategyTrades.Add(trade);
                        ErrorFx.V($"BITMEX MANUAL GROOOOVE", 0);
                    }

                    ///// TRADE SET - NOW TO PROCESSING 

                    ////////////////
                    // CLOSE POSITION 
                    if (resp.IsClose)
                    {
                        Guid exitId;
                        if (Guid.TryParse(resp.clOrdID, out exitId))
                        {
                            trade.ExitOrderID = exitId;
                        }

                        trade.ExitMessage = uniqueId == Guid.Empty ? resp.text.Clean(200) : resp.ordStatus;
                        trade.ExitPrice = resp.price;
                        trade.ExitTime = DateTime.UtcNow;
                        trade.ExitType = resp.ordType;
                        trade.ExitFee = resp.commission;

                        //profit is wrong bro, fix  
                        trade.ExitProfit = trade.ProfitValue;
                        trade.ExitProfitPercent = trade.ProfitPercent;

                        if (trade.Leverage == null || trade.BreakEvenPrice == null)
                        {
                            trade.BreakEvenPrice = mp?.breakEvenPrice;
                            trade.LiquidationPrice = mp?.liquidationPrice;
                            trade.ExitFee = mp?.commission;
                            trade.Leverage = mp?.leverage;
                        }

                        string nofy = "";
                        if (fullClose)
                        {
                            trade.StatusID = 3;
                            nofy = $"Closed {trade.EntrySignal.ToCamelCase()} position #{trade.TradeNum} as {resp.ordType} Qty: {resp.orderQty.toString("n0")} | {trade.EntryPrice.toString("n1")} => {trade.ExitPrice.toString("n1")} Diff: {trade?.ExitDiff.toString("n1")} Hope: {trade?.ProfitPercent.toString("n")}% Profit: {trade?.ProfitUSD.toString("c")} Fees: -{trade.FeesUSD.toString("c")} Net: {trade?.NetProfitUSD.toString("c")}  | {resp.text}";
                        }
                        else
                        {
                            trade.EntryMessage = "Nibbled";
                            //trade.StatusID = 2;
                            nofy = $"Nibbled {trade.EntrySignal.ToCamelCase()} position #{trade.TradeNum} Partial Qty: {resp.orderQty} | {trade.EntryPrice.toString("n1")} => {trade.ExitPrice.toString("n1")} Diff: {trade?.ExitDiff.toString("n1")} Hope: {trade?.ProfitPercent.toString("n")}% Profit: {trade?.ProfitUSD.toString("c")} Fees: -{trade.FeesUSD.toString("c")} Net: {trade?.NetProfitUSD.toString("c")}  | {resp.text}";
                        }

                        ErrorFx.V(nofy, 1);

                        //BE AS QUICK AS POSSIBLE COMMITING STATUSID CHANGES
                        await context.SaveChangesAsync();
                        ErrorFx.V($"Saved {trade.ID}", 2);

                        ////// NOTIFICATIONS
                        if (model.Preferences?.EnablePush == true)
                        {
                            OinkNotifications.SendPushNofy(nofy);
                        }

                        NotifyTrade(trade, Context.ConnectionId, model.Email, true);

                        //Stops and Takes
                        if (fullClose)
                        { 
                            await ProcessOpenOrdersAsync(br);
                        }
                        else
                        {
                            //Change Liquidation Stop at Nibble Price 
                            //Reduce by 5 points in order to avoid immediate execution 
                            br.StopPrice = resp.price + (resp.side == "Sell" ? -5 : 5);
                            br.Quantity = resp.orderQty;//since we split in halves this is ok
                            br.Instructions = "LastPrice,Close";
                            //br.Quantity = mp?.PositiveQuantity;
                            Task.Run(() => MoveStops(br));
                        }

                        ErrorFx.V($"Refreshing Balance...", 3);
                        model.WalletBalance = await GetWalletAsync(br);
                        trade.Balance = model.WalletBalance?.walletBalance * 0.00000001m;
                        //trade.ExitLastPrice = LastXbt?.lastPrice;
                        //trade.ExitMarkPrice = LastXbt?.markPrice;
                        //trade.ExitBuySize = LastBook?.Where(a => a.side == "Buy").Sum(a => a.size);
                        //trade.ExitSellSize = LastBook?.Where(a => a.side == "Sell").Sum(a => a.size);

                        //Balance and some post post processing 
                        await context.SaveChangesAsync();
                        ErrorFx.V($"Saved: {trade.ID}", 2);

                        /////
                        //BUFFER Check
                        if (model.TradeBuffer.Count > 10)
                        {
                            ErrorFx.V($"Buffer Full - Resetting Buffer", 1);
                            model.TradeBuffer.Clear();
                        }
                        else
                        {
                            ErrorFx.V($"Buffer OK Count: {model.TradeBuffer.Count} Filled: {trade?.TradeNum}", 3);
                        }

                        return;
                    }

                    ////////////////////////////
                    // New Trade Filled                

                    var nofyText = new StringBuilder();
                    if (trade.StatusID == 0)
                    {
                        trade.StatusID = 1;
                        trade.EntryAsyncFillTime = DateTime.UtcNow;
                        trade.SourceName = "SOCKET";
                        trade.EntryMessage = resp.ordStatus;
                        trade.EntryType = resp.ordType;
                        trade.EntryFee = resp.commission;

                        nofyText.Append($"New => Filled::{trade.TradeNum} T: {resp.ordType}  {resp.side} P:{resp.price} ({resp.orderQty}) {resp.text} | Fee:{resp.commission}");
                    }
                    else if (trade.StatusID == 1)
                    {
                        //realtime fill will pass here, so only flips count as pseudo close 

                        nofyText.Append($"Filled => Filled::{trade.TradeNum} T: {resp.ordType} {resp.side} P:{resp.price} ({resp.orderQty}) {resp.text} | Fee:{resp.commission}");

                        //Manual limit from mex.com and stop/takes
                        if (trade.EntrySide != resp.side)
                        {
                            if (resp.orderQty >= mp?.PositiveQuantity)
                            {
                                ErrorFx.V($"Reduced Position to Zero!", 0);
                                trade.StatusID = 3;
                            }
                            else
                            {
                                ErrorFx.V($"Partial Take - Nibble!", 0);
                                //trade.StatusID = 2;
                                trade.EntryMessage = "Nibbled";
                            }

                            if (resp.ordType == "LimitIfTouched" || resp.ordType == "MarketIfTouched")
                            {
                                nofyText.Append($" +Take");
                            }

                            if (resp.ordType == "Stop" || resp.ordType == "StopLimit" || resp.ordType == "TrailingStop")
                            {
                                nofyText.Append($" -Stop");
                            }
                        }
                    }
                    else if (trade.StatusID == 3)
                    {
                        ErrorFx.V($"Filled => Filled::Already Processed, Partial Fill or Bad Get Trade from DB? Aborting OnFill", 0);
                        return;
                    }

                    //MID SAVE - WEB SOCKET NEEDS QUICKEST POSSIBLE
                    await context.SaveChangesAsync();

                    ErrorFx.V($"Saved: {trade.ID}", 2);

                    //Add Stops
                    //Clear Stops and Takes Before Posting Anything 
                    await ProcessOpenOrdersAsync(br);


                    var positions = await PiggyBroker.GetPositionsAsync(br);
                    var position = positions?.FirstOrDefault();
                    if (position?.isOpen == true)
                    {
                        ErrorFx.V($"OPENED POSITION: {position.Side} ({position.currentQty.toString()}) Entry: {trade.EntryPrice.toString()} B/E: {position.breakEvenPrice.toString()} DIFF: {(trade.EntryPrice - position.breakEvenPrice).toString()} Liq: {position.liquidationPrice}", 1, position.Serialize());

                        trade.BreakEvenPrice = position.breakEvenPrice;
                        trade.LiquidationPrice = position.liquidationPrice;
                        trade.MarginCost = position.initMargin;
                        trade.EntryFee = position.commission;
                        trade.Leverage = position.leverage;

                        await AddStopsAsync(model, position, trade);

                        if (model.Preferences.EnableTake == true)
                        {
                            await AddTakesAsync(model, position, trade);
                        }

                        await context.SaveChangesAsync();
                        ErrorFx.V($"Saved: {trade.ID}", 2);
                    }
                    else
                    {
                        SendPositionClose(userId);
                        ErrorFx.V("Position Not Open: Not Stop Needed", 2);
                    }

                    //Buffer Help
                    context.Entry(trade).State = EntityState.Modified;

                    ////// NOTIFICATIONS
                    NotifyTrade(trade, Context.ConnectionId, model.Email);

                    //Encapsulate and refactor 
                    if (model.Preferences?.EnablePush == true && nofyText.Length > 0)
                    {
                        OinkNotifications.SendPushNofy($"{nofyText}");
                    }

                    if (position != null && !string.IsNullOrWhiteSpace(model.SignalR))
                    {
                        SendPosition(userId, position);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorFx.V($"OnFilledError!! {ex.Message}", 0, ex.StackTrace);
            }
        }
        private int getPrecision(decimal num)
        {
            var numAsStr = num.ToString("N11");
            numAsStr = System.Text.RegularExpressions.Regex.Replace(numAsStr,"/0+$/g", "");
            var precision = numAsStr.Replace(".", "").Length - num.ToString().Length;
            return precision;
        }
        /// <summary>
        /// ALWAYS PLACE STOP BEFORE LIQ, BUT ALLOW CONFIGURABLE 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="position"></param>
        /// <param name="trade"></param>
        /// <returns></returns>
        private async Task AddStopsAsync(UserServerModel model, BitmexPosition position,
            BrokerStrategiesTrade trade)
        {
            var isBuy = position.Side == "Buy";
            var br = new BrokerRequest(model.KeyId, model.KeySecret, model.LiveTurnedOn);

            br.Symbol = position.symbol;
            br.UniqueID = Guid.NewGuid();
            br.Comment = $"{trade.UniqueID}";//Used to fish out of buffer/db
            br.OrderType = "Stop";
            br.Quantity = position.currentQty ?? trade.AskQuantity;
            br.Side = isBuy ? "Sell" : "Buy";
            br.GroupById = null;
            br.IsClose = true;
            br.Instructions = "LastPrice,Close";

            //Liquidation Stop Padding 
            decimal factor = br.Symbol == "XBTUSD" ? 0.0015m : 0.005m;
            var padding = position.liquidationPrice * factor; 
            if (padding > 1)
            {
                padding = padding.ToNearestHalfDecimal();
            }

            if (!isBuy) padding = padding * -1; 

            br.StopPrice = position.liquidationPrice + padding;
            if (model.Preferences?.EnableStop == true)
            {
                decimal? stopDiff = model.Preferences.StopAt ?? (trade.EntryPrice * .025m);
                br.StopPrice = trade.EntryPrice + (isBuy ? -stopDiff : stopDiff);
                br.StopPrice = br.StopPrice.ToNearestHalfDecimal();
            }

            if (model.Preferences?.EnablePeg == true && model.Preferences.PegAt > 0)
            {
                br.StopPrice = null;
                br.PegAmount = model.Preferences.PegAt * (isBuy ? -1 : 1);//reverse?                        
            }

            if (model.Preferences.EnableStop == false && model.Preferences.EnablePeg == false)
            {
                var diff = ((position.avgEntryPrice - position.liquidationPrice) / position.avgEntryPrice) * 100;
                diff = diff > 0 ? diff : diff * -1;
                //over 100% change needed for liq... errrr no 
                if (diff > 100)
                {
                    ErrorFx.V($"UNNECESARRU DIFF: {diff} w/ {position.liquidationPrice}", 0);
                    return;
                }
            }

            OrderResponse stop = await PiggyBroker.SendBitmexAsync(br);
            var maxTries = 10;
            int i = 1;
            while (stop.Overloaded)
            {
                if (i > maxTries)
                {
                    ErrorFx.V($"Bitmex Overloaded and I give up!", 0);
                    break;
                }

                await Task.Delay(500);

                ErrorFx.V($"Bitmex Overloaded: Retry {i}", 0);
                br.NeedsReset = false;
                stop = await PiggyBroker.SendBitmexAsync(br);
                i++;
            }

            ErrorFx.V($"{stop?.ordStatus ?? stop?.ordRejReason}", 3, stop?.Serialize());

            if (stop.Success)
            {
                ErrorFx.V($"Stop Success: {br.UniqueID} = {stop.clOrdID}", 1);
                trade.StopOrderID = br.UniqueID;
                trade.StopPrice = stop.stopPx;
            }
            else
            {
                trade.StopPrice = 0;
                OinkNotifications.SendPushNofy($"ALERT - FAILED TO ADD STOP!: {stop.text}");
            }
        }

        private async Task AddTakesAsync(UserServerModel model, BitmexPosition position,
            BrokerStrategiesTrade trade)
        {
            var isBuy = position.Side == "Buy";

            //ALWAYS PLACE STOP BEFORE LIQ, BUT ALLOW CONFIGURABLE 
            var br = new BrokerRequest(model.KeyId, model.KeySecret, model.LiveTurnedOn);
            br.Symbol = position.symbol;
            br.UniqueID = Guid.NewGuid();
            br.Comment = $"{trade.UniqueID}";            
            br.Quantity = position.currentQty ?? trade.AskQuantity;
            br.Side = isBuy ? "Sell" : "Buy";
            br.GroupById = null;
            br.IsClose = true;

            decimal? takeDiff = model.Preferences.TakeAt ?? (trade.EntryPrice * .0095m);
            br.StopPrice = trade.EntryPrice + (isBuy ? takeDiff : -takeDiff);

            if (br.Symbol == "XBTUSD")
            {
                br.StopPrice = br.StopPrice.ToNearestHalfDecimal();
            }
            
            br.Price = br.StopPrice;
            br.PegAmount = null;
            br.NeedsReset = false;
            br.UniqueID = Guid.NewGuid();
            br.Comment = $"{trade.UniqueID}";
            br.OrderType = "LimitIfTouched"; //MarketIfTouched //LimitIfTouched
            br.Quantity = position.PositiveQuantity;

            if (model.Preferences.EnableSecondTake == true
                && model.Preferences.Quantity == br.Quantity)
            {
                br.Quantity = br.Quantity.ToHalf();
            }

            br.Side = isBuy ? "Sell" : "Buy";
            br.Instructions = "LastPrice,Close";

            OrderResponse take = await PiggyBroker.SendBitmexAsync(br);

            var maxTries = 10;
            int i = 1;
            while (take.Overloaded)
            {
                if (i > maxTries)
                {
                    ErrorFx.V($"Bitmex Overloaded and I give up!", 0);
                    break;
                }

                await Task.Delay(500);

                ErrorFx.V($"Bitmex Overloaded: Retry {i}", 0);
                br.NeedsReset = false;
                take = await PiggyBroker.SendBitmexAsync(br);
                i++;
            }

            ErrorFx.V($"TAKE AT {take.price}: stop: {take.stopPx} b/e: {position.breakEvenPrice} diff: {(take.price - trade.EntryPrice).toString()} status:{take?.ordStatus ?? take?.ordRejReason}",
                3, take?.Serialize());

            if (take.Success)
            {
                ErrorFx.V($"Take Success: {br.UniqueID} = {take.clOrdID}", 1);
                trade.TakeOrderID = br.UniqueID;
                trade.TakePrice = take.stopPx;

                if (model.Preferences.EnableSecondTake == true)
                {
                    takeDiff = model.Preferences.SecondTakeAt ?? (trade.EntryPrice * .022m);
                    br.StopPrice = trade.EntryPrice + (isBuy ? takeDiff : -takeDiff);
                    br.StopPrice = br.StopPrice.ToNearestHalfDecimal();
                    br.Price = br.StopPrice;

                    take = await PiggyBroker.SendBitmexAsync(br);

                    if (!take.Success)
                    {
                        take = await PiggyBroker.SendBitmexAsync(br);
                    }

                    ErrorFx.V($"2ND TAKE AT {take.price}: stop: {take.stopPx} b/e: {position.breakEvenPrice} diff: {(take.price - trade.EntryPrice).toString()} status:{take?.ordStatus ?? take?.ordRejReason}", 2, take?.Serialize());
                }
            }
            else
            {
                trade.TakePrice = 0;
                OinkNotifications.SendPushNofy($"ALERT - FAILED TO ADD TAKE!: {take.text}");
            }
        }
        private void SendPositionClose(Guid userId)
        {
            var conn = ActiveConnections.FirstOrDefault(a => a.Value == userId);
            if (!string.IsNullOrWhiteSpace(conn.Key))
            {
                Clients.Client(conn.Key).rp(new { IsOpen = false, Qty = 0, Profit = 0, ProfitPer = 0 });
            }
        }
        private void SendPosition(Guid userId, BitmexPosition pos)
        {
            var conn = ActiveConnections.FirstOrDefault(a => a.Value == userId);
            if (!string.IsNullOrWhiteSpace(conn.Key) && pos != null)
            {
                Clients.Client(conn.Key).rp(new { Symbol = pos.symbol, IsOpen = pos.isOpen, Qty = pos.currentQty, Profit = pos.unrealisedPnl, ProfitPer = pos.unrealisedPnlPcnt * 100, ProfitUSD = pos.unrealisedPnl * 0.00000001m * SocketXbt?.lastPrice ?? 0 });
            }
        }

        private async Task ProcessOpenOrdersAsync(BrokerRequest br)
        {

            var openOrders = await PiggyBroker.GetOpenOrdersAsync(br);
            //No wait, should be quick, when we are posting the cancels should be happening 
            foreach (var open in openOrders)
            {
                //&& !string.IsNullOrWhiteSpace(open.ClOrdId)
                if (open.ordType == "Stop" || open.ordType == "StopLimit" || open.ordType == "TrailingStop")
                {
                    ErrorFx.V($"Deleting Open Stop {open.side} {open.orderID}", 1);
                    DeleteOrder(br, open.orderID);
                }

                //&& !string.IsNullOrWhiteSpace(open.ClOrdId)
                if (open.ordType == "LimitIfTouched" || open.ordType == "MarketIfTouched")
                {
                    ErrorFx.V($"Deleting Open Take {open.side} {open.orderID}", 1);
                    DeleteOrder(br, open.orderID);
                }
            }
        }

        private async Task MoveStops(BrokerRequest br, bool peg = false)
        {
            ErrorFx.V($"Moving Up Stops {br.Symbol} => {br.StopPrice}", 1);

            BitmexHttpClient client = new BitmexHttpClient(br.ClientId, br.ClientKey, br.LiveNet);

            var openOrders = await client.GetOrdersAsync(br.Symbol, true);

            //No wait, should be quick, when we are posting the cancels should be happening 
            var list = openOrders.Where(a => a.symbol == br.Symbol).ToList();
            if (list.Count == 0)
            {
                ErrorFx.V($"No open orders, abort", 1);
                return;
            }

            foreach (var open in list)
            {
                if (open.ordType == "Stop" || open.ordType == "StopLimit")
                {
                    ErrorFx.V($"Moving Up Stop {open.side} {open.stopPx} => {br.StopPrice}", 1);
                    var resp = await client.AmendOrderAsync(open.orderID, br);
                    var maxTries = 10;
                    int i = 1;
                    while (resp.Overloaded)
                    {
                        if (i > maxTries)
                        {
                            ErrorFx.V($"Move Stop - Overload give up", 0);
                            break;
                        }

                        await Task.Delay(500);

                        ErrorFx.V($"Move Overloaded: Retry {i}", 0);
                        br.NeedsReset = false;
                        resp = await client.AmendOrderAsync(open.orderID, br);
                        i++;
                    }
                }
            }

            if (peg)
            {
                //open.ordType == "TrailingStop"
            }
        }

        private static void DeleteOrder(BrokerRequest br, string stopOrderID)
        {
            Task.Run(async () =>
            {
                BitmexHttpClient client = new BitmexHttpClient(br.ClientId, br.ClientKey, br.LiveNet);
                string del = await client.DeleteOrderAsync(stopOrderID);
            });
        }



    }
}
