using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Piggy;
using Newtonsoft.Json;

namespace Bitmex
{
    public class BitmexHttpClient
    {
        private const string testnet = "https://testnet.bitmex.com";
        private const string livenet = "https://www.bitmex.com";
        private string apiKey;
        private string apiSecret;
        private int rateLimit;
        public bool LiveTurnedOn = false;
        private string Domain => LiveTurnedOn ? livenet : testnet;

        public BitmexHttpClient(string bitmexKey = "", string bitmexSecret = "", bool? live = false, int rateLimit = 5000)
        {
            this.apiKey = bitmexKey;
            this.apiSecret = bitmexSecret;
            this.rateLimit = rateLimit;
            this.LiveTurnedOn = live == true;
        }

        public void Set(string bitmexKey, string bitmexSecret, bool live)
        {
            this.apiKey = bitmexKey;
            this.apiSecret = bitmexSecret;
            this.LiveTurnedOn = live;
        }

        private string BuildQueryData(Dictionary<string, string> param)
        {
            if (param == null)
                return "";

            StringBuilder b = new StringBuilder();
            foreach (var item in param)
                b.Append(string.Format("&{0}={1}", item.Key, WebUtility.UrlEncode(item.Value)));

            try { return b.Length > 0 ? b.ToString().Substring(1) : ""; }
            catch (Exception) { return ""; }
        }


        private string BuildJSON(Dictionary<string, string> param)
        {
            if (param == null)
                return "";

            var entries = new List<string>();
            foreach (var item in param)
                entries.Add($"\"{item.Key}\":\"{item.Value}\"");

            return "{" + string.Join(",", entries) + "}";
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private long GetNonce()
        {
            DateTime yearBegin = new DateTime(1990, 1, 1);
            return DateTime.UtcNow.Ticks - yearBegin.Ticks;
        }

        private string Query(string method, string function, Dictionary<string, string> param = null, bool auth = false, bool json = false)
        {
            string paramData = json ? BuildJSON(param) : BuildQueryData(param);
            string url = "/api/v1" + function + ((method == "GET" && paramData != "") ? "?" + paramData : "");
            string postData = (method != "GET") ? paramData : "";

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Domain + url);
            webRequest.Method = method;

            if (auth)
            {
                string nonce = GetNonce().ToString();
                string message = method + url + nonce + postData;
                byte[] signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(apiSecret), Encoding.UTF8.GetBytes(message));
                string signatureString = ByteArrayToString(signatureBytes);

                webRequest.Headers.Add("api-nonce", nonce);
                webRequest.Headers.Add("api-key", apiKey);
                webRequest.Headers.Add("api-signature", signatureString);
            }

            try
            {
                if (postData != "")
                {
                    webRequest.ContentType = json ? "application/json" : "application/x-www-form-urlencoded";
                    var data = Encoding.UTF8.GetBytes(postData);
                    using (var stream = webRequest.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }

                using (WebResponse webResponse = webRequest.GetResponse())
                using (Stream str = webResponse.GetResponseStream())
                using (StreamReader sr = new StreamReader(str))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                using (HttpWebResponse response = (HttpWebResponse)wex.Response)
                {
                    if (response == null)
                        throw;

                    using (Stream str = response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(str))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        private async Task<T> HttpAsync<T>(string method, string function, Dictionary<string, string> param = null,
            bool auth = false, bool json = false) where T : class
        {
            var text = "";
            try
            {
                text = await HttpAsync(method, function, param, auth, json).ConfigureAwait(false);
                if (text?.Contains("error") == true)
                {
                    ErrorFx.V("BitErr::" + function + param != null ? BuildJSON(param) : null, 0, text);
                }
                else if (text?.Contains("Bad Gateway") == true)
                {
                    //<html>  <head><title>502 Bad Gateway</title></head>  <body bgcolor="white">  <center><h1>502 Bad Gateway</h1></center>  </body>  </html>
                    ErrorFx.V("HttpAsync-BadGateway::" + function + param != null ? BuildJSON(param) : null, 0, text);
                }
                else if (text?.Contains("overloaded") == true)
                {
                    ErrorFx.V("HttpAsync-Overloaded::" + function + param != null ? BuildJSON(param) : null, 0, text);
                }

                return text.Deserialize<T>();
            }
            catch (Exception ex)
            {
                ErrorFx.Log(ex.Message, ex.StackTrace, $"{method}/{function}/{text}|" + (param != null ? BuildJSON(param) : ""), 1);
                return null;
            }
            //return JsonConvert.DeserializeObject<T>(await HttpAsync(method, function, param, auth, json));
        }


        /// <summary>
        /// Change to HttpClient Static
        /// </summary>
        /// <param name="method"></param>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="auth"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        private async Task<string> HttpAsync(string method, string function, Dictionary<string, string> param = null, bool auth = false, bool json = false)
        {
            string paramData = json ? BuildJSON(param) : BuildQueryData(param);
            string url = "/api/v1" + function + ((method == "GET" && paramData != "") ? "?" + paramData : "");
            string postData = (method != "GET") ? paramData : "";

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Domain + url);
            webRequest.Method = method;

            var expires = DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeSeconds().ToString();

            if (auth)
            {
                string message = method + url + expires + postData;
                byte[] signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(apiSecret), Encoding.UTF8.GetBytes(message));
                string signatureString = ByteArrayToString(signatureBytes);

                webRequest.Headers.Add("api-expires", expires);
                webRequest.Headers.Add("api-key", apiKey);
                webRequest.Headers.Add("api-signature", signatureString);
            }

            try
            {
                if (postData != "")
                {
                    webRequest.ContentType = json ? "application/json" : "application/x-www-form-urlencoded";
                    var data = Encoding.UTF8.GetBytes(postData);
                    using (var stream = await webRequest.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }

                using (WebResponse webResponse = await webRequest.GetResponseAsync())
                using (Stream str = webResponse.GetResponseStream())
                using (StreamReader sr = new StreamReader(str))
                {
                    return await sr.ReadToEndAsync();
                }
            }
            catch (WebException wex)
            {
                using (HttpWebResponse response = (HttpWebResponse)wex.Response)
                {
                    if (response == null)
                        throw;

                    using (Stream str = response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(str))
                        {
                            return await sr.ReadToEndAsync();
                        }
                    }
                }
            }
        }
 
        public async Task<List<OrderResponse>> GetOrdersAsync(string symbol, bool openOnly = false, int limit = 10)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = symbol;
            param["reverse"] = "1";
            param["count"] = limit.ToString();

            if (openOnly)
            {
                param["filter"] = "{\"open\":1}";
            }

            //param["columns"] = "";
            //param["startTime"] = "";
            //param["endTime"] = "";

            return await HttpAsync<List<OrderResponse>>("GET", "/order", param, true);
        }

        /// <summary>
        /// Rework http response into http
        /// </summary>
        /// <returns></returns>
        public async Task<BitmexHttpResponse<List<BitmexPosition>>> GetPositionsAsync()
        {
            var response = new BitmexHttpResponse<List<BitmexPosition>>();

            var param = new Dictionary<string, string>();
            
            var text = await HttpAsync("GET", "/position", param, true);
            try
            {
                if (text?.Contains("error") == true)
                {
                    ErrorFx.V("GetPosition-Error:" + param != null ? BuildJSON(param) : null, 0, text);
                    response.error = new BitmexError(text);
                }
                else if (text?.Contains("Bad Gateway") == true)
                {
                    response.error = new BitmexError("bad gateway");
                    //<html>  <head><title>502 Bad Gateway</title></head>  <body bgcolor="white">  <center><h1>502 Bad Gateway</h1></center>  </body>  </html>
                    ErrorFx.V("GetPosition-BadGateway::" + param != null ? BuildJSON(param) : null, 0, text);
                }
                else if (text?.Contains("overloaded") == true)
                {
                    response.error = new BitmexError("overloaded");
                    ErrorFx.V("GetPosition-Overloaded::" + param != null ? BuildJSON(param) : null, 0, text);
                }

                response.Result = text.Deserialize<List<BitmexPosition>>();
            }
            catch (Exception ex)
            {                
                ErrorFx.V($"Position Error: {ex.Message}", 0, $"{text}");
                response.error = new BitmexError(ex.Message);
            }
            return response;
        }

        public async Task<OrderResponse> PostOrdersAsync(BrokerRequest req, string overrideInstructions = null)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = req.Symbol;
            param["side"] = req.Side;
            param["orderQty"] = req.Quantity.ToString();

            //Limit = orderQty and price 
            //Stop = orderQty, stopPx 
            //StopLimit = orderQty, stopPx, price 
            param["ordType"] = req.OrderType;

            if (req.Hidden)
            {
                //hidden orders pay taker fee fuuuuck
                //note: only hidden orders can be amended!
                param["displayQty"] = "0";
            }

            if (req.OrderType == "LimitIfTouched" || req.OrderType == "StopLimit")
            {
                //*required: round to nearest .5 decimal 
                if (req.Price > 0)
                {
                    var pricer = Convert.ToDecimal(Math.Round((double)(req.Price * 2)) / 2);
                    param["price"] = pricer.ToString(CultureInfo.InvariantCulture);
                }

                if (req.StopPrice > 0)
                {
                    var stopr = Convert.ToDecimal(Math.Round((double)(req.StopPrice * 2)) / 2);
                    param["stopPx"] = stopr.ToString(CultureInfo.InvariantCulture);
                }

                if (req.PegAmount > 0 || req.PegAmount < 0)
                {
                    param["pegOffsetValue"] = req.PegAmount.toString();
                }
            }
            else if (req.OrderType == "Limit")
            {
                var pricer = Convert.ToDecimal(Math.Round((double)(req.Price * 2)) / 2);
                param["price"] = pricer.ToString(CultureInfo.InvariantCulture);
            }
            else if (req.OrderType == "Stop" || req.OrderType == "MarketIfTouched")
            {
                if (req.StopPrice > 0)
                {
                    var stopr = Convert.ToDecimal(Math.Round((double)(req.StopPrice * 2)) / 2);
                    param["stopPx"] = stopr.ToString(CultureInfo.InvariantCulture);
                }

                if (req.PegAmount > 0 || req.PegAmount < 0)
                {
                    param["pegOffsetValue"] = req.PegAmount.toString();
                }
            }

            if (req.UniqueID != null)
            {
                param["clOrdID"] = (req.NeedsReset ? Guid.NewGuid() : req.UniqueID).ToString();
            }

            if (!string.IsNullOrWhiteSpace(req.GroupById))
            {
                //link by run or strategy 
                param["clOrdLinkID"] = req.GroupById;
                param["contingencyType"] = "OneCancelsTheOther";
            }

            if (!string.IsNullOrWhiteSpace(req.Comment))
                param["text"] = req.Comment;

            if (!string.IsNullOrWhiteSpace(req.TimeInForce))
                param["timeInForce"] = req.TimeInForce;

            if (req.IsClose)
            {
                param["execInst"] = "Close";
            }

            //Override 1
            if (!string.IsNullOrWhiteSpace(req.Instructions))
                param["execInst"] = req.Instructions;

            //ParticipateDoNotInitiate
            if (!string.IsNullOrWhiteSpace(overrideInstructions))
            {
                //ImmediateOrCancel
                //Override 2
                param["execInst"] = overrideInstructions;

                if (param.ContainsKey("clOrdLinkID"))
                    param.Remove("clOrdLinkID");

                if (param.ContainsKey("contingencyType"))
                    param.Remove("contingencyType");

                if (param.ContainsKey("contingencyType"))
                    param.Remove("contingencyType");

                //On testnet i can use FillOrKill.... hmmm 
                if (overrideInstructions == "ParticipateDoNotInitiate" && param.ContainsKey("timeInForce"))
                    param.Remove("timeInForce");

            }

            req.NeedsReset = true;

            ErrorFx.V($"HttpPost: {req.OrderType} {req.Side} {req.Price.toString()} {req.TryLogic} {overrideInstructions}", 3, "vars: " + param.Serialize());

            return await HttpAsync<OrderResponse>("POST", "/order", param, true);
        }

        public async Task<OrderResponse> PostOrdersBulkAsync(BrokerRequest req, string overrideInstructions = null)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = req.Symbol;
            param["side"] = req.Side;
            param["orderQty"] = req.Quantity.ToString();

            //Limit = orderQty and price 
            //Stop = orderQty, stopPx 
            //StopLimit = orderQty, stopPx, price 
            param["ordType"] = req.OrderType;

            if (req.Hidden)
            {
                //hidden orders pay taker fee fuuuuck
                //note: only hidden orders can be amended!
                param["displayQty"] = "0";
            }

            if (req.OrderType == "LimitIfTouched" || req.OrderType == "StopLimit")
            {
                //*required: round to nearest .5 decimal 
                if (req.Price > 0)
                {
                    var pricer = Convert.ToDecimal(Math.Round((double)(req.Price * 2)) / 2);
                    param["price"] = pricer.ToString(CultureInfo.InvariantCulture);
                }

                if (req.StopPrice > 0)
                {
                    var stopr = Convert.ToDecimal(Math.Round((double)(req.StopPrice * 2)) / 2);
                    param["stopPx"] = stopr.ToString(CultureInfo.InvariantCulture);
                }

                if (req.PegAmount > 0 || req.PegAmount < 0)
                {
                    param["pegOffsetValue"] = req.PegAmount.toString();
                }
            }
            else if (req.OrderType == "Limit")
            {
                var pricer = Convert.ToDecimal(Math.Round((double)(req.Price * 2)) / 2);
                param["price"] = pricer.ToString(CultureInfo.InvariantCulture);
            }
            else if (req.OrderType == "Stop" || req.OrderType == "MarketIfTouched")
            {
                if (req.StopPrice > 0)
                {
                    var stopr = Convert.ToDecimal(Math.Round((double)(req.StopPrice * 2)) / 2);
                    param["stopPx"] = stopr.ToString(CultureInfo.InvariantCulture);
                }

                if (req.PegAmount > 0 || req.PegAmount < 0)
                {
                    param["pegOffsetValue"] = req.PegAmount.toString();
                }
            }

            if (req.UniqueID != null)
            {
                param["clOrdID"] = (req.NeedsReset ? Guid.NewGuid() : req.UniqueID).ToString();
            }

            if (!string.IsNullOrWhiteSpace(req.GroupById))
            {
                //link by run or strategy 
                param["clOrdLinkID"] = req.GroupById;
                param["contingencyType"] = "OneCancelsTheOther";
            }

            if (!string.IsNullOrWhiteSpace(req.Comment))
                param["text"] = req.Comment;

            if (!string.IsNullOrWhiteSpace(req.TimeInForce))
                param["timeInForce"] = req.TimeInForce;

            if (req.IsClose)
            {
                param["execInst"] = "Close";
            }

            //Override 1
            if (!string.IsNullOrWhiteSpace(req.Instructions))
                param["execInst"] = req.Instructions;

            //ParticipateDoNotInitiate
            if (!string.IsNullOrWhiteSpace(overrideInstructions))
            {
                //ImmediateOrCancel
                //Override 2
                param["execInst"] = overrideInstructions;

                if (param.ContainsKey("clOrdLinkID"))
                    param.Remove("clOrdLinkID");

                if (param.ContainsKey("contingencyType"))
                    param.Remove("contingencyType");

                if (param.ContainsKey("contingencyType"))
                    param.Remove("contingencyType");

                //On testnet i can use FillOrKill.... hmmm 
                if (overrideInstructions == "ParticipateDoNotInitiate" && param.ContainsKey("timeInForce"))
                    param.Remove("timeInForce");

            }

            req.NeedsReset = true;

            ErrorFx.V($"HttpPost: {req.OrderType} {req.Side} {req.Price.toString()} {req.TryLogic} {overrideInstructions}", 3, "vars: " + param.Serialize());

            if (req.HasOpenPosition)
            {
                //Create Close Order add to List 
            }

            //Send List 
            return await HttpAsync<OrderResponse>("POST", "/order/bulk", param, true);
        }


        public async Task<OrderResponse> AmendOrderAsync(BrokerRequest req, bool reduceToZero, OrderResponse resp)
        {
            var param = new Dictionary<string, string>();

            param["orderID"] = resp.orderID;
            //param["origClOrdID"] = req.UniqueID.ToString();

            //According to docs we can change price and qty within 60 secs 
            if (reduceToZero)
            {
                param["leavesQty"] = "0";
            }
            else
            {
                param["orderQty"] = req.Quantity.ToString();
                param["price"] = req.Price.ToString();
            }

            if (!string.IsNullOrWhiteSpace(req.Comment))
                param["text"] = req.Comment;

            if (!string.IsNullOrWhiteSpace(req.Instructions))
                param["execInst"] = req.Instructions;

            //ParticipateDoNotInitiate

            return await HttpAsync<OrderResponse>("PUT", "/order", param, true);
        }

        public async Task<OrderResponse> AmendOrderAsync(string orderId, BrokerRequest req)
        {
            var param = new Dictionary<string, string>();

            param["orderID"] = orderId;            

            //According to docs we can change price and qty within 60 secs 
            if (req.Quantity == 0)
            {
                param["leavesQty"] = "0";
            }
            else
            {
                param["orderQty"] = req.Quantity.ToString();                
            }

            if (req.StopPrice > 0)
            {
                param["stopPx"] = req.StopPrice.ToString();
            }

            if (req.Price > 0)
            {
                param["price"] = req.Price.ToString();
            }

            if (!string.IsNullOrWhiteSpace(req.Comment))
                param["text"] = req.Comment;

            if (!string.IsNullOrWhiteSpace(req.Instructions))
                param["execInst"] = req.Instructions;

            return await HttpAsync<OrderResponse>("PUT", "/order", param, true);
        }

        public async Task<string> DeleteOrderAsync(string orderId)
        {
            var param = new Dictionary<string, string>();
            param["orderID"] = orderId;
            //param["clOrdID"] = orderId;
            //param["origClOrdID"] = req.UniqueID.ToString();

            return await HttpAsync("DELETE", "/order", param, true);
        }

        public async Task<BitmexWallet> GetWalletAsync()
        {
            var param = new Dictionary<string, string>();
            //param["side"] = side;
            return await HttpAsync<BitmexWallet>("GET", "/user/wallet", param, true);
        }

        public async Task<List<BitmexWalletSummary>> GetWalletSummaryAsyncOLD()
        {
            var param = new Dictionary<string, string>();
            return await HttpAsync<List<BitmexWalletSummary>>("GET", "/user/walletSummary", param, true);
        }

        public async Task<BitmexHttpResponse<List<BitmexWalletSummary>>> GetWalletSummaryAsync()
        {
            var response = new BitmexHttpResponse<List<BitmexWalletSummary>>();

            var param = new Dictionary<string, string>();

            var text = await HttpAsync("GET", "/user/walletSummary", param, true);
            try
            {
                if (text?.Contains("error") == true)
                {
                    ErrorFx.V("GetWallet-Error:" + param != null ? BuildJSON(param) : null, 0, text);
                    response.error = new BitmexError(text);
                }
                else if (text?.Contains("Bad Gateway") == true)
                {
                    response.error = new BitmexError("bad gateway");
                    //<html>  <head><title>502 Bad Gateway</title></head>  <body bgcolor="white">  <center><h1>502 Bad Gateway</h1></center>  </body>  </html>
                    ErrorFx.V("GetWallet-BadGateway::" + param != null ? BuildJSON(param) : null, 0, text);
                }
                else if (text?.Contains("overloaded") == true)
                {
                    response.error = new BitmexError("overloaded");
                    ErrorFx.V("GetWallet-Overloaded::" + param != null ? BuildJSON(param) : null, 0, text);
                }

                response.Result = text.Deserialize<List<BitmexWalletSummary>>();
            }
            catch (Exception ex)
            {
                ErrorFx.V($"Position Error: {ex.Message}", 0, $"{text}");
                response.error = new BitmexError(ex.Message);
            }
            return response;
        }



        //https://www.bitmex.com/api/v1/instrument?symbol=XBT&count=100&reverse=false
        public async Task<BitmexInstrument> GetInstrumentAsync(string symbol)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = symbol;
            var result = await HttpAsync<List<BitmexInstrument>>("GET", "/instrument", param, false).ConfigureAwait(false);
            return result.FirstOrDefault();
        }

        public async Task<List<BitmexInstrument>> GetInstrumentsAsync()
        {
            var param = new Dictionary<string, string>();            
            var result = await HttpAsync<List<BitmexInstrument>>("GET", "/instrument/active", param, false).ConfigureAwait(false);
            return result;
        }

        public async Task<List<BitmexOrderBook>> GetBookAsync(string symbol, int depth = 25)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = symbol;
            param["depth"] = depth.ToString();
            return await HttpAsync<List<BitmexOrderBook>>("GET", "/orderBook/L2", param, false);
        }

        //var apiInstance = new OrderApi();
        //var symbol = symbol_example;  // string | Instrument symbol. e.g. 'XBTUSD'.
        //var side = side_example;  // string | Order side. Valid options: Buy, Sell. Defaults to 'Buy' unless `orderQty` or `simpleOrderQty` is negative. (optional) 
        //var simpleOrderQty = 1.2;  // double? | Order quantity in units of the underlying instrument (i.e. Bitcoin). (optional) 
        //var orderQty = 8.14;  // decimal? | Order quantity in units of the instrument (i.e. contracts). (optional) 
        //var price = 1.2;  // double? | Optional limit price for 'Limit', 'StopLimit', and 'LimitIfTouched' orders. (optional) 
        //var displayQty = 8.14;  // decimal? | Optional quantity to display in the book. Use 0 for a fully hidden order. (optional) 
        //var stopPx = 1.2;  // double? | Optional trigger price for 'Stop', 'StopLimit', 'MarketIfTouched', and 'LimitIfTouched' orders. Use a price below the current price for stop-sell orders and buy-if-touched orders. Use `execInst` of 'MarkPrice' or 'LastPrice' to define the current price used for triggering. (optional) 
        //var clOrdID = clOrdID_example;  // string | Optional Client Order ID. This clOrdID will come back on the order and any related executions. (optional) 
        //var clOrdLinkID = clOrdLinkID_example;  // string | Optional Client Order Link ID for contingent orders. (optional) 
        //var pegOffsetValue = 1.2;  // double? | Optional trailing offset from the current price for 'Stop', 'StopLimit', 'MarketIfTouched', and 'LimitIfTouched' orders; use a negative offset for stop-sell orders and buy-if-touched orders. Optional offset from the peg price for 'Pegged' orders. (optional) 
        //var pegPriceType = pegPriceType_example;  // string | Optional peg price type. Valid options: LastPeg, MidPricePeg, MarketPeg, PrimaryPeg, TrailingStopPeg. (optional) 
        //var ordType = ordType_example;  // string | Order type. Valid options: Market, Limit, Stop, StopLimit, MarketIfTouched, LimitIfTouched, MarketWithLeftOverAsLimit, Pegged. Defaults to 'Limit' when `price` is specified. Defaults to 'Stop' when `stopPx` is specified. Defaults to 'StopLimit' when `price` and `stopPx` are specified. (optional)  (default to Limit)
        //var timeInForce = timeInForce_example;  // string | Time in force. Valid options: Day, GoodTillCancel, ImmediateOrCancel, FillOrKill. Defaults to 'GoodTillCancel' for 'Limit', 'StopLimit', 'LimitIfTouched', and 'MarketWithLeftOverAsLimit' orders. (optional) 
        //var execInst = execInst_example;  // string | Optional execution instructions. Valid options: ParticipateDoNotInitiate, AllOrNone, MarkPrice, IndexPrice, LastPrice, Close, ReduceOnly, Fixed. 'AllOrNone' instruction requires `displayQty` to be 0. 'MarkPrice', 'IndexPrice' or 'LastPrice' instruction valid for 'Stop', 'StopLimit', 'MarketIfTouched', and 'LimitIfTouched' orders. (optional) 
        //var contingencyType = contingencyType_example;  // string | Optional contingency type for use with `clOrdLinkID`. Valid options: OneCancelsTheOther, OneTriggersTheOther, OneUpdatesTheOtherAbsolute, OneUpdatesTheOtherProportional. (optional) 
        //var text = text_example;  // string | Optional order annotation. e.g. 'Take profit'. (optional) 

        public string DeleteOrders()
        {
            var param = new Dictionary<string, string>();
            param["orderID"] = "de709f12-2f24-9a36-b047-ab0ff090f0bb";
            param["text"] = "cancel order by ID";
            return Query("DELETE", "/order", param, true, true);
        }




        private byte[] hmacsha256(byte[] keyByte, byte[] messageBytes)
        {
            using (var hash = new HMACSHA256(keyByte))
            {
                return hash.ComputeHash(messageBytes);
            }

        }


        private long lastTicks = 0;
        private object thisLock = new object();

        private void RateLimit()
        {
            lock (thisLock)
            {
                long elapsedTicks = DateTime.Now.Ticks - lastTicks;
                var timespan = new TimeSpan(elapsedTicks);
                if (timespan.TotalMilliseconds < rateLimit)
                    Thread.Sleep(rateLimit - (int)timespan.TotalMilliseconds);
                lastTicks = DateTime.Now.Ticks;
            }
        }


    }
}