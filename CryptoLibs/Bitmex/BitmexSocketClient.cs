 
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bitmex;
using Bitmex.Client.Websocket.Responses;
using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Implementation;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Logging;
using Binance.Net.Converters;
using Binance.Net.Objects;
using OrderStatus = Binance.Net.Objects.OrderStatus;

namespace Piggy
{


    //// Table name / Subscription topic.
    //// Could be "trade", "order", "instrument", etc.
    //"table": string,

    //// The type of the message. Types:
    //// 'partial'; This is a table image, replace your data entirely.
    //// 'update': Update a single row.
    //// 'insert': Insert a new row.
    //// 'delete': Delete a row.
    //"action": 'partial' | 'update' | 'insert' | 'delete',

    //// An array of table rows is emitted here. They are identical in structure to data returned from the REST API.
    //"data": Object[],

    ////
    //// The below fields define the table and are only sent on a `partial`
    ////

    //// Attribute names that are guaranteed to be unique per object.
    //// If more than one is provided, the key is composite.
    //// Use these key names to uniquely identify rows. Key columns are guaranteed
    //// to be present on all data received.
    //"keys"?: string[],

    //// This lists key relationships with other tables.
    //// For example, `quote`'s foreign key is {"symbol": "instrument"}
    //"foreignKeys"?: {[key: string]: string
    //},

    //// This lists the shape of the table. The possible types:
    //// "symbol" - In most languages this is equal to "string"
    //// "guid"
    //// "timestamp"
    //// "timespan"
    //// "float"
    //// "long"
    //// "integer"
    //// "boolean"
    //"types"?: {[key: string]: string},

    //// When multiple subscriptions are active to the same table, use the `filter` to correlate which datagram
    //// belongs to which subscription, as the `table` property will not contain the subscription's symbol.
    //"filter"?: {account?: number, symbol?: string},

    //// These are internal fields that indicate how responses are sorted and grouped.
    //"attributes"?: {[key: string]: string},

    public class BitmexStreamEvent
    {
        public BitmexAction action { get; set; }
        public string table { get; set; }
        public string[] keys { get; set; }
        public Dictionary<string, string> types { get; set; }
        public Dictionary<string, string> foreignKeys { get; set; }
        public Dictionary<string, string> attributes { get; set; }
        public FilterInfo filter { get; set; }
        public string data { get; set; }
    }

    public class BitmexStreamMessage<T>
    {
        public BitmexAction action { get; set; }
        public string table { get; set; }
        public string[] keys { get; set; }
        public Dictionary<string, string> types { get; set; }
        public Dictionary<string, string> foreignKeys { get; set; }
        public Dictionary<string, string> attributes { get; set; }
        public FilterInfo filter { get; set; }
        public IEnumerable<T> data { get; set; }
        public T First => data != null ? data.FirstOrDefault() : default(T);
    }

    public class BitmexStreamOrderUpdate : BitmexStreamEvent
    {
        /// <summary>
        /// The symbol the order is for
        /// </summary>
        [JsonProperty("s")]
        public string Symbol { get; set; }
        /// <summary>
        /// The new client order id
        /// </summary>
        [JsonProperty("c")]
        public string ClientOrderId { get; set; }
        /// <summary>
        /// The side of the order
        /// </summary>
        [JsonProperty("S"), JsonConverter(typeof(OrderSideConverter))]
        public OrderSide Side { get; set; }
        /// <summary>
        /// The type of the order
        /// </summary>
        [JsonProperty("o"), JsonConverter(typeof(OrderTypeConverter))]
        public OrderType Type { get; set; }
        /// <summary>
        /// The timespan the order is active
        /// </summary>
        [JsonProperty("f"), JsonConverter(typeof(TimeInForceConverter))]
        public TimeInForce TimeInForce { get; set; }
        /// <summary>
        /// The quantity of the order
        /// </summary>
        [JsonProperty("q")]
        public decimal Quantity { get; set; }
        /// <summary>
        /// The price of the order
        /// </summary>
        [JsonProperty("p")]
        public decimal Price { get; set; }
        [JsonProperty("P")]
        public decimal StopPrice { get; set; }
        [JsonProperty("F")]
        public decimal IcebergQuantity { get; set; }
        [JsonProperty("g")]
        public decimal g { get; set; }
        [JsonProperty("C")]
        public object OriginalClientOrderId { get; set; }
        /// <summary>
        /// The execution type
        /// </summary>
        [JsonProperty("x"), JsonConverter(typeof(ExecutionTypeConverter))]
        public ExecutionType ExecutionType { get; set; }
        /// <summary>
        /// The status of the order
        /// </summary>
        [JsonProperty("X"), JsonConverter(typeof(OrderStatusConverter))]
        public OrderStatus Status { get; set; }
        /// <summary>
        /// The reason the order was rejected
        /// </summary>
        [JsonProperty("r"), JsonConverter(typeof(OrderRejectReasonConverter))]
        public OrderRejectReason RejectReason { get; set; }
        /// <summary>
        /// The id of the order as assigned by Binance
        /// </summary>
        [JsonProperty("i")]
        public long OrderId { get; set; }
        /// <summary>
        /// The quantity of the last filled trade of this order
        /// </summary>
        [JsonProperty("l")]
        public decimal QuantityOfLastFilledTrade { get; set; }
        /// <summary>
        /// The quantity of all trades that were filled for this order
        /// </summary>
        [JsonProperty("z")]
        public decimal AccumulatedQuantityOfFilledTrades { get; set; }
        /// <summary>
        /// The price of the last filled trade
        /// </summary>
        [JsonProperty("L")]
        public decimal PriceLastFilledTrade { get; set; }
        /// <summary>
        /// The commission payed
        /// </summary>
        [JsonProperty("n")]
        public decimal Commission { get; set; }
        /// <summary>
        /// The asset the commission was taken from
        /// </summary>
        [JsonProperty("N")]
        public string CommissionAsset { get; set; }
        /// <summary>
        /// The time of the update
        /// </summary>
        [JsonProperty("T"), JsonConverter(typeof(TimestampConverter))]
        public DateTime Time { get; set; }
        /// <summary>
        /// The trade id
        /// </summary>
        [JsonProperty("t")]
        public long TradeId { get; set; }
        [JsonProperty("I")]
        public long I { get; set; }
        [JsonProperty("w")]
        public bool IsWorking { get; set; }
        /// <summary>
        /// Whether the buyer is the maker
        /// </summary>
        [JsonProperty("m")]
        public bool BuyerIsMaker { get; set; }

        [JsonProperty("O")]
        public object O { get; set; }
    }


    public class BitmexStream
    {
        private readonly HMACSHA256 encryptor;

        public BitmexStream() { }
        public BitmexStream(string secret)
        {
            encryptor = new HMACSHA256(Encoding.ASCII.GetBytes(secret));
        }

        internal bool TryReconnect { get; set; } = true;
        public IWebsocket Socket { get; set; }
        public BitmexStreamSubscription StreamResult { get; set; }

        public async Task Close()
        {
            TryReconnect = false;
            await Socket.Close();
        }
    }

    public class BitmexStreamSubscription
    {
        /// <summary>
        /// Event when the socket is closed
        /// </summary>
        public event Action Closed;

        /// <summary>
        /// Event when an error occures on the socket
        /// </summary>
        public event Action<Exception> Error;

        internal int StreamId { get; set; }

        internal void InvokeClosed()
        {
            Closed?.Invoke();
        }

        internal void InvokeError(Exception ex)
        {
            Error?.Invoke(ex);
        }
    }
    public class BitmexSocketClientOptions : ExchangeOptions
    {
        /// <summary>
        /// The base adress for the socket connections
        /// </summary>
        public string BaseSocketAddress { get; set; } = "wss://www.bitmex.com/realtime";

        /// <summary>
        /// What should be done when the connection is interupted
        /// </summary>
        public ReconnectBehaviour ReconnectTryBehaviour { get; set; } = ReconnectBehaviour.AutoReconnect;

        /// <summary>
        /// The interval to try to reconnect the websocket after the connection was lost
        /// </summary>
        public TimeSpan ReconnectTryInterval { get; set; } = TimeSpan.FromSeconds(5);
    }

    public class BitmexAuthenticationProvider : AuthenticationProvider
    {
        private readonly HMACSHA256 encryptor;

        public BitmexAuthenticationProvider(ApiCredentials credentials) : base(credentials)
        {
            encryptor = new HMACSHA256(Encoding.ASCII.GetBytes(credentials.Secret));
        }

        public override string AddAuthenticationToUriString(string uri, bool signed)
        {
            if (!signed)
                return uri;

            if (!uri.Contains("?"))
                uri += "?";

            var query = uri.Split('?');

            if (!uri.EndsWith("?"))
                uri += "&";

            uri += $"signature={ByteToString(encryptor.ComputeHash(Encoding.UTF8.GetBytes(query.Length > 1 ? query[1] : "")))}";
            return uri;
        }


        //# Generates an API signature.
        //# A signature is HMAC_SHA256(secret, verb + path + nonce + data), hex encoded.
        //# Verb must be uppercased, url is relative, nonce must be an increasing 64-bit integer
        //# and the data, if present, must be JSON without whitespace between keys.
        //        def bitmex_signature(apiSecret, verb, url, nonce, postdict= None):
        //            """Given an API Secret key and data, create a BitMEX-compatible signature."""
        //        data = ''
        //        if postdict:
        //# separators remove spaces from json
        //# BitMEX expects signatures from JSON built without spaces
        //        data = json.dumps(postdict, separators=(',', ':'))
        //        parsedURL = urllib.parse.urlparse(url)
        //            path = parsedURL.path
        //                if parsedURL.query:
        //        path = path + '?' + parsedURL.query
        //# print("Computing HMAC: %s" % verb + path + str(nonce) + data)
        //            message = (verb + path + str(nonce) + data).encode('utf-8')
        //        print("Signing: %s" % str(message))

        //        signature = hmac.new(apiSecret.encode('utf-8'), message, digestmod=hashlib.sha256).hexdigest()
        //        print("Signature: %s" % signature)
        //        return signature


        private string Sign(string uri, long expires)
        {
            return ByteToString(encryptor.ComputeHash(Encoding.UTF8.GetBytes($"GET{uri}{expires}")));
        }

 
        public override IRequest AddAuthenticationToRequest(IRequest request, bool signed)
        {
            var expires = DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeSeconds();
            var signatureString = Sign(request.Uri.ToString(), expires);

            request.Headers.Add("api-expires", expires.ToString());
            request.Headers.Add("api-key", Credentials.Key);
            request.Headers.Add("api-signature", signatureString);
            
            return request;
        }
       
    }

    public class BitmexSocketClient : ExchangeClient
    {
        #region fields
        private static BitmexSocketClientOptions defaultOptions = new BitmexSocketClientOptions();

        private string baseWebsocketAddress;
        private ReconnectBehaviour reconnectBehaviour;
        private TimeSpan reconnectInterval;

        private readonly List<BitmexStream> sockets = new List<BitmexStream>();

        private int lastStreamId;
        private readonly object streamIdLock = new object();
        private SslProtocols protocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

        private const string DepthStreamEndpoint = "@orderBook2";
        private const string InstrumentStreamEndpoint = "@instrument";
        private const string TradesStreamEndpoint = "@trade";
        private const string AggregatedTradesStreamEndpoint = "@tradeBin";
        private const string PartialBookDepthStreamEndpoint = "@quote";

        private const string AccountUpdateEvent = "position";
        private const string ExecutionUpdateEvent = "execution";
        #endregion

        #region properties
        public IWebsocketFactory SocketFactory { get; set; } = new WebsocketFactory();
        #endregion

        #region constructor/destructor

        /// <summary>
        /// Create a new instance of BitmexSocketClient with default options
        /// </summary>
        public BitmexSocketClient() : this(defaultOptions)
        {

        }

        /// <summary>
        /// Create a new instance of BitmexSocketClient using provided options
        /// </summary>
        /// <param name="options">The options to use for this client</param>
        public BitmexSocketClient(BitmexSocketClientOptions options) : base(options, options.ApiCredentials == null ? null : new BitmexAuthenticationProvider(options.ApiCredentials))
        {
            Configure(options);
        }
        #endregion 

        #region methods
        /// <summary>
        /// Set the default options to be used when creating new socket clients
        /// </summary>
        /// <param name="options"></param>
        public static void SetDefaultOptions(BitmexSocketClientOptions options)
        {
            defaultOptions = options;
        }

        /// <summary>
        /// Set the API key and secret
        /// </summary>
        /// <param name="apiKeyNameId">The api key</param>
        /// <param name="apiSecretValue">The api secret</param>
        public void SetApiCredentials(string apiKeyNameId, string apiSecretValue)
        {
            SetAuthenticationProvider(new BitmexAuthenticationProvider(new ApiCredentials(apiKeyNameId, apiSecretValue)));
        }

        /// <summary>
        /// Synchronized version of the <see cref="SubscribeToInstrumentStreamAsync"/> method
        /// </summary>
        /// <returns></returns>
        public CallResult<BitmexStreamSubscription> SubscribeToInstrumentStream(string symbol, Action<BitmexInstrument> onMessage) => SubscribeToInstrumentStreamAsync(symbol, onMessage).Result;

        /// <summary>
        /// Subscribes to the candlestick update stream for the provided symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="interval">The interval of the candlesticks</param>
        /// <param name="onMessage">The event handler for the received data</param>
        /// <returns>A stream subscription. This stream subscription can be used to be notified when the socket is closed and can close this specific stream 
        /// using the <see cref="UnsubscribeFromStream(BitmexStreamSubscription)"/> method</returns>
        public async Task<CallResult<BitmexStreamSubscription>> SubscribeToInstrumentStreamAsync(string symbol, Action<BitmexInstrument> onMessage)
        {
            symbol = symbol.ToLower();
            var socketResult = await CreateSocket(baseWebsocketAddress + symbol + InstrumentStreamEndpoint + "_" + "xyz").ConfigureAwait(false);
            if (!socketResult.Success)
                return new CallResult<BitmexStreamSubscription>(null, socketResult.Error);

            socketResult.Data.Socket.OnMessage += (msg) =>
            {
                var result = Deserialize<BitmexInstrument>(msg, false);
                if (result.Success)
                    onMessage?.Invoke(result.Data);
                else
                    log.Write(LogVerbosity.Warning, "Couldn't deserialize data received from Instrument stream: " + result.Error);
            };

            log.Write(LogVerbosity.Info, $"Started Instrument stream for {symbol}");
            return new CallResult<BitmexStreamSubscription>(socketResult.Data.StreamResult, null);
        }




        /// <summary>
        /// Synchronized version of the <see cref="SubscribeToPartialBookDepthStreamAsync"/> method
        /// </summary>
        /// <returns></returns>
        public CallResult<BitmexStreamSubscription> SubscribeToPartialBookDepthStream(string symbol, int levels, Action<BitmexOrderBook> onMessage) => SubscribeToPartialBookDepthStreamAsync(symbol, levels, onMessage).Result;

        /// <summary>
        /// Subscribes to the depth updates
        /// </summary>
        /// <param name="symbol">The symbol to subscribe on</param>
        /// <param name="levels">The amount of entries to be returned in the update</param>
        /// <param name="onMessage">The event handler for the received data</param>
        /// <returns>A stream subscription. This stream subscription can be used to be notified when the socket is closed and can close this specific stream 
        /// using the <see cref="UnsubscribeFromStream(BitmexStreamSubscription)"/> method</returns>
        public async Task<CallResult<BitmexStreamSubscription>> SubscribeToPartialBookDepthStreamAsync(string symbol, int levels, Action<BitmexOrderBook> onMessage)
        {
            symbol = symbol.ToLower();
            var socketResult = await CreateSocket(baseWebsocketAddress + symbol + PartialBookDepthStreamEndpoint + levels).ConfigureAwait(false);
            if (!socketResult.Success)
                return new CallResult<BitmexStreamSubscription>(null, socketResult.Error);

            socketResult.Data.Socket.OnMessage += (msg) =>
            {
                var result = Deserialize<BitmexOrderBook>(msg, false);
                if (result.Success)
                    onMessage?.Invoke(result.Data);
                else
                    log.Write(LogVerbosity.Warning, "Couldn't deserialize data received from depth stream: " + result.Error);
            };

            log.Write(LogVerbosity.Info, "Started partial book depth stream");
            return new CallResult<BitmexStreamSubscription>(socketResult.Data.StreamResult, null);
        }

        /// <summary>
        /// Synchronized version of the <see cref="SubscribeToUserStreamAsync"/> method
        /// </summary>
        /// <returns></returns>
        public CallResult<BitmexStreamSubscription> SubscribeToUserStream(string listenKey, Action<BitmexStreamOrderUpdate> onOrderUpdateMessage) => SubscribeToUserStreamAsync(listenKey, onOrderUpdateMessage).Result;

        /// <summary>
        /// Subscribes to the account update stream. Prior to using this, the <see cref="BitmexClient.StartUserStream"/> method should be called.
        /// </summary>
        /// <param name="listenKey">Listen key retrieved by the StartUserStream method</param>
        /// <param name="onAccountInfoMessage">The event handler for whenever an account info update is received</param>
        /// <param name="onOrderUpdateMessage">The event handler for whenever an order status update is received</param>
        /// <returns>A stream subscription. This stream subscription can be used to be notified when the socket is closed and can close this specific stream 
        /// using the <see cref="UnsubscribeFromStream(BitmexStreamSubscription)"/> method</returns>
        public async Task<CallResult<BitmexStreamSubscription>> SubscribeToUserStreamAsync(string listenKey, Action<BitmexStreamOrderUpdate> onOrderUpdateMessage)
        {
            if (string.IsNullOrEmpty(listenKey))
                return new CallResult<BitmexStreamSubscription>(null, new ArgumentError("ListenKey must be provided"));

            return await CreateUserStream(listenKey, onOrderUpdateMessage).ConfigureAwait(false);
        }

        /// <summary>
        /// Unsubscribes from a stream
        /// </summary>
        /// <param name="streamSubscription">The stream subscription received by subscribing</param>
        public void UnsubscribeFromStream(BitmexStreamSubscription streamSubscription)
        {
            lock (sockets)
                sockets.SingleOrDefault(s => s.StreamResult.StreamId == streamSubscription.StreamId)?.Close().Wait();
        }

        /// <summary>
        /// Unsubscribes from all streams
        /// </summary>
        public void UnsubscribeAllStreams()
        {
            lock (sockets)
                sockets.ToList().ForEach(s => s.Close().Wait());
        }

        /// <summary>
        /// Dispose this instance
        /// </summary>
        public override void Dispose()
        {
            log.Write(LogVerbosity.Info, "Disposing socket client, closing sockets");
            lock (sockets)
                sockets.ToList().ForEach(s => s.Socket.Close());
        }

        //"affiliate",   // Affiliate status, such as total referred users & payout %
        //"execution",   // Individual executions; can be multiple per order
        //"order",       // Live updates on your orders
        //"margin",      // Updates on your current account balance and margin requirements
        //"position",    // Updates on your positions
        //"privateNotifications", // Individual notifications - currently not used
        //"transact"     // Deposit/Withdrawal updates
        //"wallet"       // Bitcoin address balance data, including total deposits & withdrawals

        private async Task<CallResult<BitmexStreamSubscription>> CreateUserStream(string listenKey, Action<BitmexStreamOrderUpdate> onOrderUpdateMessage)
        {
            var socketResult = await CreateSocket(baseWebsocketAddress + listenKey).ConfigureAwait(false);
            if (!socketResult.Success)
                return new CallResult<BitmexStreamSubscription>(null, socketResult.Error);

            socketResult.Data.Socket.OnMessage += (msg) =>
            {
                if (msg.Contains(AccountUpdateEvent))
                {
                    //var result = Deserialize<BitmexStreamAccountInfo>(msg, false);
                    //if (result.Success)
                    //    onAccountInfoMessage?.Invoke(result.Data);
                    //else
                    //    log.Write(LogVerbosity.Warning, "Couldn't deserialize data received from account stream: " + result.Error);
                }
                else if (msg.Contains(ExecutionUpdateEvent))
                {
                    log.Write(LogVerbosity.Debug, msg);
                    var result = Deserialize<BitmexStreamOrderUpdate>(msg, false);
                    if (result.Success)
                        onOrderUpdateMessage?.Invoke(result.Data);
                    else
                        log.Write(LogVerbosity.Warning, "Couldn't deserialize data received from order stream: " + result.Error);
                }
            };

            log.Write(LogVerbosity.Info, "User stream started");
            return new CallResult<BitmexStreamSubscription>(socketResult.Data.StreamResult, null);
        }

        private async Task<CallResult<BitmexStream>> CreateSocket(string url)
        {
            try
            {
                var socket = SocketFactory.CreateWebsocket(url);
                var socketObject = new BitmexStream() { Socket = socket, StreamResult = new BitmexStreamSubscription() { StreamId = NextStreamId() } };
                socket.SetEnabledSslProtocols(protocols);

                socket.OnClose += () => Socket_OnClose(socketObject);

                socket.OnError += Socket_OnError;
                socket.OnError += socketObject.StreamResult.InvokeError;

                socket.OnOpen += Socket_OnOpen;
                var connected = await socket.Connect().ConfigureAwait(false);
                if (!connected)
                {
                    log.Write(LogVerbosity.Error, "Couldn't open socket stream");
                    return new CallResult<BitmexStream>(null, new CantConnectError());
                }

                log.Write(LogVerbosity.Debug, "Socket connection established");

                lock (sockets)
                    sockets.Add(socketObject);
                return new CallResult<BitmexStream>(socketObject, null);
            }
            catch (Exception e)
            {
                var errorMessage = $"Couldn't open socket stream: {e.Message}";
                log.Write(LogVerbosity.Error, errorMessage);
                return new CallResult<BitmexStream>(null, new CantConnectError());
            }
        }

        private void Configure(BitmexSocketClientOptions options)
        {
            baseWebsocketAddress = options.BaseSocketAddress;
            reconnectBehaviour = options.ReconnectTryBehaviour;
            reconnectInterval = options.ReconnectTryInterval;
        }

        private void Socket_OnOpen()
        {
            log.Write(LogVerbosity.Debug, "Socket opened");
        }

        private void Socket_OnError(Exception e)
        {
            log.Write(LogVerbosity.Error, $"Socket error {e?.Message}");
        }

        private void Socket_OnClose(object sender)
        {
            var con = (BitmexStream)sender;
            if (reconnectBehaviour == ReconnectBehaviour.AutoReconnect && con.TryReconnect)
            {
                log.Write(LogVerbosity.Info, "Connection lost, going to try to reconnect");
                Task.Run(() =>
                {
                    Thread.Sleep((int)Math.Round(reconnectInterval.TotalMilliseconds));
                    if (con.Socket.Connect().Result)
                        log.Write(LogVerbosity.Info, "Reconnected");
                });
            }
            else
            {
                log.Write(LogVerbosity.Info, "Socket closed");
                con.StreamResult.InvokeClosed();
                con.Socket.Dispose();
                lock (sockets)
                    sockets.Remove(con);
            }
        }

        private int NextStreamId()
        {
            lock (streamIdLock)
            {
                lastStreamId++;
                return lastStreamId;
            }
        }
        #endregion
    }
}
