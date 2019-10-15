using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Piggy
{
    public class ManualTradeData
    {
        //Symbol 
        public string Instrument { get; set; }
        public bool IsLong { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
    }

    public class BrokerRequest
    {
        public Guid? UniqueID { get; set; }
        public string GroupById { get; set; }

        public int ExchangeId { get; set; }
        public string ClientId { get; set; }
        public string ClientKey { get; set; }
        public string Symbol { get; set; }
        public decimal? Price { get; set; }
        public string OrderType { get; set; }
        public decimal? Quantity { get; set; }
        public string Side { get; set; }
        public string Comment { get; set; }
        //eliminate 
        public bool BuyMarket => OrderType == "Market";
        public bool HasOpenPosition { get; set; }
        public string Instructions { get; set; }
        public string TryLogic { get; set; }
        public bool Hidden { get; set; }
        public bool NeedsReset { get; set; }
        public string TimeInForce { get; set; }
        public bool LiveNet { get; set; }
        public bool IsKeyTest { get; set; }
        public bool IsOneShot => OrderType == "Stop" || OrderType == "Market" || OrderType == "LimitIfTouched" || OrderType == "MarketIfTouched" || OrderType == "StopLimit" || OrderType == "MarketWithLeftOverAsLimit";
        public bool IsClose { get; set; }
        public decimal? StopPrice { get; set; }
        public decimal? PegAmount { get; set; }

        public BrokerRequest() { }

        public BrokerRequest(string id,
            string key,bool liveTurnedOn) {
            ClientId = id;
            ClientKey = key;
            LiveNet = liveTurnedOn;

            OrderType = "Limit";
            UniqueID = Guid.NewGuid();
        }

        public BrokerRequest(
            string id,
            string key,
            string symbol,
            decimal? price,
            decimal? qty,
            string side,
            string comment,
            string orderType,
            string instructions = null)
        {
            ClientId = id;
            ClientKey = key;
            Symbol = symbol;
            Price = price;
            Quantity = qty;
            Side = side;
            Comment = comment;
            OrderType = orderType;
            Instructions = instructions;
        }

        public BrokerRequest New()
        {
            var r = new BrokerRequest();
            r.OrderType = "Limit";
            r.UniqueID = Guid.NewGuid();
            r.ClientKey = ClientKey;
            r.ClientId = ClientId;
            r.LiveNet = LiveNet;
            r.Symbol = Symbol;
            r.Side = Side;
            r.Quantity = Quantity;
            r.Price = Price;
            r.StopPrice = StopPrice;


            return r;
        }


    }
    public class BrokerUser
    {
        public Guid ID { get; set; }
        public string Email { get; set; }
        public string TradingViewUser { get; set; }
        public string ChromeId { get; set; }
        public string Mobile { get; set; }
        public string TestID { get; set; }
        public string TestKey { get; set; }
        public string LiveID { get; set; }
        public string LiveKey { get; set; }
        public bool? Enabled { get; set; }
        public bool? LiveTurnedOn { get; set; }
        public DateTime? DateTimeUpdated { get; set; }
        public DateTime? DateTimeCreated { get; set; }

        public DateTime? DateTimeConnected { get; set; }
        public DateTime? DateTimeDisconnected { get; set; }


        public string SignalR { get; set; }
        public string IPAddress { get; set; }
        public int? LastStrategyID { get; set; }
        public int? LastRunID { get; set; }
        public int? StatusID { get; set; }
        public string OneSignalID { get; set; }
    }

    public class BrokerPreference
    {
        public int ID { get; set; }
        public Guid? FK_UserID { get; set; }
        public string Symbol { get; set; }
        public bool? EnableLong { get; set; }
        public bool? EnableShort { get; set; }
        public int? Quantity { get; set; }
        public bool? EnableMarket { get; set; }
        public string LimitOptionId { get; set; }
        public bool? EnableAutoClose { get; set; }
        public bool? EnablePush { get; set; }
        public bool? EnableEmail { get; set; }
        public bool? EnableSound { get; set; }
        public bool? EnableTake { get; set; }
        public decimal? TakeAt { get; set; }
        public bool? EnableStop { get; set; }
        public decimal? StopAt { get; set; }
        public bool? EnablePeg { get; set; }
        public decimal? PegAt { get; set; }
        public bool? EnableSame { get; set; }
        public int? MaxSame { get; set; }

        public bool? EnableSecondTake { get; set; }
        public decimal? SecondTakeAt { get; set; }
        public bool? EnableMultiply { get; set; }
        public int? MultiplyFactor { get; set; }
        public bool? EnableGel { get; set; }
        public int? GelBy { get; set; }
        public bool? EnableResetOnChop { get; set; }
        public bool? EnableRiskyFlips { get; set; }

        public bool? EnableGreed { get; set; }
        public DateTime? DateTimeUpdated { get; set; }

    }

    public class BrokerStrategy
    {
        public int ID { get; set; }
        public Guid? FK_UserID { get; set; }
        public string StrategyName { get; set; }

        //prefs body repeat on purpose 
        public bool? EnableLong { get; set; }
        public bool? EnableShort { get; set; }
        public bool? EnableAutoClose { get; set; }
        public bool? EnablePush { get; set; }
        public bool? EnableEmail { get; set; }
        public bool? EnableMarket { get; set; }
        public string LimitOptionId { get; set; }
        public int? Quantity { get; set; }

        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string ChartId { get; set; }
        public int? CandleInterval { get; set; }
        public string StrategyId { get; set; }
        public int? ChromeTabId { get; set; }

        public int? StatusID { get; set; }
        public DateTime? DateTimeCreated { get; set; }
        public DateTime? DateTimeUpdated { get; set; }

    }
    public class BrokerStrategiesRun
    {
        public int ID { get; set; }
        public Guid? FK_UserID { get; set; }
        public int? FK_StrategyID { get; set; }
        public DateTime? DateTimeStarted { get; set; }
        public DateTime? DateTimeStopped { get; set; }
        public DateTime? DateTimePaused { get; set; }
        public string StartStrategyName { get; set; }
        public string StartChartId { get; set; }
        public int? StartInterval { get; set; }
        public int? StartQuantity { get; set; }
        public int? StartTradeNum { get; set; }
        public int? BacktestCount { get; set; }
        public decimal? EstimatedProfitPercent { get; set; }
        public decimal? EstimatedProfitRatio { get; set; }

    }

    public class BrokerStrategiesTrade
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public Guid? UniqueID { get; set; }
        public Guid? FK_UserID { get; set; }
        public int? FK_StrategyID { get; set; }
        public int? FK_RunID { get; set; }

        public string Symbol { get; set; }
        public int? TradeNum { get; set; }
        public int? StatusID { get; set; }
        public decimal? AskPrice { get; set; }
        public decimal? AskQuantity { get; set; }
        public string AskType { get; set; }
        public string EntrySignal { get; set; }
        public string EntryMessage { get; set; }
        public decimal? EntryPrice { get; set; }
        public decimal? EntryQuantity { get; set; }
        public string EntryType { get; set; }
        public DateTimeOffset? EntryTime { get; set; }
        public DateTimeOffset? EntryAsyncFillTime { get; set; }
        public decimal? EntryFee { get; set; }
        public string ExchangeOrderID { get; set; }
        public string RequestCode { get; set; }
        public string ResponseCode { get; set; }
        public string ExchangeError { get; set; }
        public Guid? ExitOrderID { get; set; }
        public Guid? ReplacedByID { get; set; }
        public string ExitType { get; set; }
        public string ExitMessage { get; set; }
        public decimal? ExitPrice { get; set; }
        public DateTimeOffset? ExitTime { get; set; }
        public decimal? ExitFee { get; set; }
        public decimal? ExitProfit { get; set; }
        public decimal? ExitProfitPercent { get; set; }
        public bool? CancelledPrevious { get; set; }
        public DateTime? DateTimeCreated { get; set; }
        public string SourceName { get; set; }
        public bool? Success { get; set; }
        // ReSharper disable once UnusedMember.Global
        public bool? Archive { get; set; }
        public Guid? StopOrderID { get; set; }
        public decimal? StopPrice { get; set; }
        public Guid? TakeOrderID { get; set; }
        public decimal? TakePrice { get; set; }
        public decimal? BreakEvenPrice { get; set; }
        public decimal? LiquidationPrice { get; set; }
        public decimal? MarginCost { get; set; }
        public int? Leverage { get; set; } = 10;
        public decimal? Balance { get; set; }
        public decimal? LastPrice { get; set; }//quiero saber si es mejor tv askprice or mex lastprice/markprice
        public decimal? MarkPrice { get; set; }
        public long? BuySize { get; set; }
        public long? SellSize { get; set; }

        //UNMAPPED
        public bool EntryLong => EntrySignal?.ToLower().Contains("long") == true;
        public bool EntryShort => EntrySignal?.ToLower().Contains("short") == true;
        public string EntrySide => EntryShort ? "Sell" : "Buy";
        public decimal? EntryDiff => AskPrice > 0 && EntryPrice > 0 ? ((AskPrice - EntryPrice) / AskPrice) * 100 : null;
        public decimal? EntrySplippage => (EntryDiff / EntryPrice) * 100;
        public decimal? ExitDiff => (EntryShort ? (EntryPrice - ExitPrice) : (ExitPrice - EntryPrice));
        public decimal? Fees => EntryFee + ExitFee;
        public decimal? Profit => EntryQuantity * ExitDiff - Fees;
 
        //Merge
        public decimal? EntryValue => EntryQuantity / EntryPrice;
        public decimal? EntryValueUSD => EntryValue* EntryPrice;
        public decimal? Margin => 1 /Leverage * EntryValue + (0.0015m* EntryValue);
        public decimal? MarginUSD => Margin* EntryPrice;
        public decimal? ExitValue => EntryQuantity / ExitPrice;
        public decimal? ExitValueUSD => ExitValue* ExitPrice;
        //public decimal? FeesValue => EntryValue* 0.0015m;
        public decimal? FeesValue => (EntryValue*(EntryFee ?? 0.00075m)) + (ExitValue*(ExitFee ?? 0.00075m));
        public decimal? FeesUSD => (EntryValue* (EntryFee ?? 0.00075m)) * EntryPrice + (ExitValue*(ExitFee ?? 0.00075m) * ExitPrice); 

        //value is inverse, it decreases with longs 
        public decimal? ProfitValue => (EntryLong ? (EntryValue - ExitValue) : (ExitValue - EntryValue)); 
        public decimal? ProfitUSD => ProfitValue* ExitPrice;
        public decimal? NetProfit => ProfitValue - FeesValue;
        public decimal? NetProfitUSD => NetProfit* ExitPrice;
        public decimal? ProfitPercent => ProfitValue/EntryValue* 100;

                 

        public void Sanitize()
        {
            if (EntryMessage?.Length > 200)
                EntryMessage = EntryMessage.Clean(200);
            if (ExchangeError?.Length > 500)
                ExchangeError = ExchangeError.Clean(500);
            if (SourceName?.Length > 50)
                SourceName = SourceName.Clean(50);
            if (RequestCode?.Length > 1500)
                RequestCode = RequestCode.Clean(1500);
            if (ResponseCode?.Length > 1500)
                ResponseCode = ResponseCode.Clean(1500);

        }
    }
}
