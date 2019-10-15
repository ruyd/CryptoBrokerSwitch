using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy
{

    public class BitmexOrderItem
    {
        public string orderID { get; set; }
        public string clOrdID { get; set; }
        public string clOrdLinkID { get; set; }
        public int account { get; set; }
        public string symbol { get; set; }
        public string side { get; set; }
        public decimal? simpleOrderQty { get; set; }
        public decimal? orderQty { get; set; }
        public decimal? price { get; set; }
        public decimal? displayQty { get; set; }
        public decimal? stopPx { get; set; }
        public decimal? pegOffsetValue { get; set; }
        public string pegPriceType { get; set; }
        public string currency { get; set; }
        public string settlCurrency { get; set; }
        public string ordType { get; set; }
        public string timeInForce { get; set; }
        public string execInst { get; set; }
        public string contingencyType { get; set; }
        public string exDestination { get; set; }
        public string ordStatus { get; set; }
        public string triggered { get; set; }
        public bool workingIndicator { get; set; }
        public string ordRejReason { get; set; }
        public decimal? simpleLeavesQty { get; set; }
        public decimal? leavesQty { get; set; }
        public decimal? simpleCumQty { get; set; }
        public decimal? cumQty { get; set; }
        public decimal? avgPx { get; set; }
        public string multiLegReportingType { get; set; }
        public string text { get; set; }
        public DateTime? transactTime { get; set; }
        public DateTime? timestamp { get; set; }
    }
 
    public class BitmexOrderBook
    {
        //{"symbol":"XBTUSD","id":17999992000,"side":"Sell","size":100,"price":80}
        public string symbol { get; set; }
        public string id { get; set; }
        public string side { get; set; }
        public int? size { get; set; }
        public decimal? price { get; set; }
    }


    //[0,"43ca9f13-26f6-41e2-b6f2-d1757e4e79c7","priv",{"table":"execution","action":"insert","data":[
    //{"execID":"3d9da94a-90d4-863b-2ff8-14f411a3c77d","orderID":"21defb57-7f71-eecd-b26e-3f88cc6201c6","clOrdID":"8e31de8c-457d-4de4-aaa2-3a0c198e4b91","clOrdLinkID":"XBTUSD",
    //"account":595181,"symbol":"XBTUSD","side":"Buy","lastQty":20,"lastPx":6296.5,"underlyingLastPx":null,"lastMkt":"XBME","lastLiquidityInd":"RemovedLiquidity","simpleOrderQty":null,
    //"orderQty":20,"price":6296.5,"displayQty":null,"stopPx":null,"pegOffsetValue":null,"pegPriceType":"","currency":"USD","settlCurrency":"XBt","execType":"Trade","ordType":"Limit",
    //"timeInForce":"GoodTillCancel","execInst":"","contingencyType":"OneCancelsTheOther","exDestination":"XBME","ordStatus":"Filled","triggered":"","workingIndicator":false,"ordRejReason":"",
    //"simpleLeavesQty":0,"leavesQty":0,"simpleCumQty":0.0031764,"cumQty":20,"avgPx":6296.5,"commission":0.00075,"tradePublishIndicator":"PublishTrade","multiLegReportingType":"SingleSecurity",
    //"text":"Ruy OCC5) Buy# 222","trdMatchID":"50896d30-6be1-6eb8-26d9-6fa0c97d90a5","execCost":-317640,"execComm":238,"homeNotional":0.0031764,"foreignNotional":-20,"transactTime":"2018-08-16T04:47:07.914Z","timestamp":"2018-08-16T04:47:07.914Z"}]}]
    public class OrderResponse
    {

        public string execID { get; set; }
        public string lastQty { get; set; }
        public string lastPx { get; set; }
        public string underlyingLastPx { get; set; }
        public string trdMatchID { get; set; }
        public decimal? commission { get; set; }
        public string execCost { get; set; }
        public decimal? execComm { get; set; }


        //////// EXECUTION ITEMS ABOVE 
        //Guid
        public string orderID { get; set; }
        public string clOrdID { get; set; }
        public string clOrdLinkID { get; set; }
        public int account { get; set; }
        public string symbol { get; set; }
        public string side { get; set; }
        public decimal? simpleOrderQty { get; set; }
        public decimal? orderQty { get; set; }
        public decimal? price { get; set; }
        public decimal? displayQty { get; set; }
        public decimal? stopPx { get; set; }
        public decimal? pegOffsetValue { get; set; }
        public string pegPriceType { get; set; }
        public string currency { get; set; }
        public string settlCurrency { get; set; }
        public string ordType { get; set; }
        public string timeInForce { get; set; }
        public string execInst { get; set; }
        public bool IsClose => execInst?.Contains("Close") == true;
        public string contingencyType { get; set; }
        public string exDestination { get; set; }
        public string ordStatus { get; set; }
        public string triggered { get; set; }
        public bool? workingIndicator { get; set; }
        public string ordRejReason { get; set; }
        public decimal? simpleLeavesQty { get; set; }
        public decimal? leavesQty { get; set; }
        public decimal? simpleCumQty { get; set; }
        public decimal? cumQty { get; set; }
        public decimal? avgPx { get; set; }
        public string multiLegReportingType { get; set; }
        public string text { get; set; }
        public DateTimeOffset? transactTime { get; set; }
        public DateTime? timestamp { get; set; }
        public bool Success => !string.IsNullOrWhiteSpace(orderID) && ordStatus != "Canceled" && ordStatus != "Cancelled";
        public bool Overloaded => error?.message?.Contains("overloaded") == true;
        public BitmexError error { get; set; }

        public static OrderResponse Error(string s, int? c, BrokerStrategiesTrade t = null)
        {
            var o = new OrderResponse();
            o.ordStatus = s;
            o.ordRejReason = s;
            o.error = new BitmexError() { message = s, code = c };

            o.price = t?.AskPrice;
            o.orderQty = t?.AskQuantity;
            o.ordType = t?.AskType;

            return o;
        }
    }

    public class BitmexHttpResponse<T>
    {
        public T Result { get; set; }
        public bool Overloaded => error?.message?.Contains("overloaded") == true;
        public BitmexError error { get; set; }
        public bool Success => string.IsNullOrWhiteSpace(error?.message);

    }

    public class BitmexError
    {
        public string name { get; set; }
        public string message { get; set; }
        public int? code { get; set; }

        public BitmexError() { }
        public BitmexError(string _message)
        {
            message = _message;
        }
    }

    public class BitmexWallet
    {
        public int? account { get; set; }
        public string currency { get; set; }
        public decimal? prevDeposited { get; set; }
        public decimal? prevWithdrawn { get; set; }
        public decimal? prevTransferIn { get; set; }
        public decimal? prevTransferOut { get; set; }
        public decimal? prevAmount { get; set; }
        public DateTime? prevTimestamp { get; set; }
        public decimal? deltaDeposited { get; set; }
        public decimal? deltaWithdrawn { get; set; }
        public decimal? deltaTransferIn { get; set; }
        public decimal? deltaTransferOut { get; set; }
        public decimal? deltaAmount { get; set; }
        public decimal? deposited { get; set; }
        public decimal? withdrawn { get; set; }
        public decimal? transferIn { get; set; }
        public decimal? transferOut { get; set; }
        public decimal? amount { get; set; }
        public decimal? pendingCredit { get; set; }
        public decimal? pendingDebit { get; set; }
        public decimal? confirmedDebit { get; set; }
        public DateTime? timestamp { get; set; }
        public string addr { get; set; }
        public string script { get; set; }
        public List<string> withdrawalLock { get; set; }
    }

    public class BitmexWalletSummary
    {
        public int? account { get; set; }
        public string currency { get; set; }
        public string transactType { get; set; }
        public string symbol { get; set; }
        public decimal? amount { get; set; }
        public decimal? pendingDebit { get; set; }
        public decimal? realisedPnl { get; set; }
        public decimal? walletBalance { get; set; }
        public decimal? unrealisedPnl { get; set; }
        public decimal? marginBalance { get; set; }
        
    }

    public class BitMexStream
    {
        public string table;
        public string action;
        public List<string> keys;
    }


    public class BitMexStreamDepthSnap
    {
        public string table;
        public string action;
        public List<string> keys;
        public List<BitMexDepthCache> data;
    }


    public class BitMexDepthCache
    {
        public string symbol;
        public long timestamp;

        public List<object[]> bids = new List<object[]>();
        public List<object[]> asks = new List<object[]>();
    }


    public class BitMexStreamDepth
    {
        public string table;
        public string action;
        //public List<string> keys;
        public List<BitMexDepth> data;
    }


    public class BitMexDepth
    {
        public string symbol;
        public int level;
        public int? bidSize;
        public decimal? bidPrice;
        public int? askSize;
        public decimal? askPrice;
        public long timestamp;

        public override string ToString()
        {
            return symbol + " " + level + " " + bidSize + " @ " + bidPrice + " / " + askSize + " @ " + askPrice;
        }
    }


    public class BitMexInstrumentShort
    {
        public string symbol;
        public string rootSymbol;
        public string state;
        public string typ;
        public string listing;
        public string expiry;
        public string underlying;
        public string buyLeg;
        public string sellLeg;
        public string quoteCurrency;
        public string reference;
        public string referenceSymbol;
        public decimal tickSize;
        public long multiplier;
        public string settlCurrency;
        public decimal initMargin;
        public decimal maintMargin;
        public decimal limit;
        public string openingTimestamp;
        public string closingTimestamp;
        public decimal prevClosePrice;
        public decimal limitDownPrice;
        public decimal limitUpPrice;
        public decimal volume;
        public bool isQuanto;
        public bool isInverse;
        public decimal totalVolume;
        public decimal vwap;
        public decimal openInterest;
        public string underlyingSymbol;
        public decimal underlyingToSettleMultiplier;
        public decimal highPrice;
        public decimal lowPrice;
        public decimal lastPrice;
        //...
    }


    public class BitMexStreamTrade
    {
        public string table;
        public string action;
        public List<string> keys;
        public List<BitMexTrade> data;
    }


    public class BitMexTrade
    {
        public long timestamp;
        public string symbol;
        public decimal size;
        public decimal price;
    }


    public class BitMexIndex
    {
        public string timestamp;
        public string symbol;
        public string side;
        public decimal size;
        public decimal price;
        public string tickDirection;
    }

    public class MarketDataSnapshot
    {
        public string Product;

        //BBO
        public decimal Bid { get { return BidDepth.Count > 0 ? BidDepth[0].Price : 0M; } }
        public decimal BidVol { get { return BidDepth.Count > 0 ? BidDepth[0].Qty : 0M; } }
        public decimal Ask { get { return AskDepth.Count > 0 ? AskDepth[0].Price : 0M; } }
        public decimal AskVol { get { return AskDepth.Count > 0 ? AskDepth[0].Qty : 0M; } }

        //Full depth
        public List<MarketDepth> BidDepth = new List<MarketDepth>();
        public List<MarketDepth> AskDepth = new List<MarketDepth>();

        public MarketDataSnapshot(string product)
        {
            Product = product;
        }

        public override string ToString()
        {
            return Product + "{ " + BidVol + "@" + Bid + " / " + Ask + "@" + AskVol + "} ";
        }
    }

    public class MarketDepth
    {
        public decimal Price;
        public decimal Qty;

        public MarketDepth() { }

        public MarketDepth(decimal price, decimal qty)
        {
            Price = price;
            Qty = qty;
        }
    }

}
