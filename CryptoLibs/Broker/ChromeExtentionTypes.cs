using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Piggy 
{
    public static partial class ActionEx
    {
        public static UserSettings ToSettings(this BrokerUser u, bool hideKeys = true)
        {
            var s = new UserSettings();
            s.LiveID = u.LiveID;
            s.LiveKey = hideKeys && u.LiveKey != null ? "dummy" : u.LiveKey;
            s.LiveTurnedOn = u.LiveTurnedOn;
            s.Mobile = u.Mobile;
            s.TestID = u.TestID;
            s.TestKey = hideKeys && u.TestKey != null ? "dummy" : u.TestKey;
            return s;        
        }

  
    }

    public class ChromeExtModel
    {
        public Guid? ID { get; set; }
        public bool? Authed { get; set; }
        public string Message { get; set; }  
        public int StatusID { get; set;  }
        public int? StrategyID { get; set; }
        public bool? Existing { get; set; }
        public UserSettings Settings { get; set; }
        public BrokerPreference Preferences { get; set; } 
        public bool? Enabled { get; set; }
        public dynamic Data { get; set; }
        public List<BrokerStrategiesTrade> BrokeredTrades { get; set; }
    }
    public class UserSettings
    {
        public string TestID { get; set; }
        public string TestKey { get; set; }
        public string LiveID { get; set; }
        public string LiveKey { get; set; }
        public bool? LiveTurnedOn { get; set; }
        public string Mobile { get; set; }
    }    
    public class ChromeIn
    {
        public string TradingViewUser { get; set; }
        public string ChromeEmail { get; set; }
        public string ChromeId { get; set; }
        public string StrategyName { get; set; }
        public int? Candle { get; set; }
        public string ChartID { get; set; }
        public int? Interval { get; set; }
        public int? LastTradeNum { get; set; }
        
    }
    public class FormPost<T>
    {
        public Guid? ID { get; set; }
        public string ChromeEmail { get; set; }
        public string ChromeId { get; set; }
        public T Data { get; set; }
        public string IPAddress { get; set; }
        public string StragegyId { get; set; }//for pref + stra double save
    }
    public class FormResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class NewOrderPost
    {
        public Guid? ID { get; set; }
        public string ChromeEmail { get; set; }
        public string ChromeId { get; set; }
        public TradingViewData Data { get; set; }
        public string IPAddress { get; set; }
    }
    public class TradingViewData
    {
        public string strategyId { get; set; }
        public string strategyName { get; set; }
        public string currency { get; set; }
        public string symbol { get; set; }
        public int? candle { get; set; }
        public string user { get; set; }
        public string chart { get; set; }
        public List<RawOrder> orders { get; set; }
        public List<RawTrade> trades { get; set; }
        public string url { get; set; }
        public int? tabId { get; set; }
    }

    public class BrokeredItem
    {
  
        public Guid? UniqueID { get; set; }
        public int? ID { get; set; }
        public int? TradeNum { get; set; }
        public string Signal { get; set; }
        public decimal? Price { get; set; }
        public decimal? Qty { get; set; }
        public decimal? Slip { get; set; }
        public int? Status { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public bool Success { get; set; }
        public DateTimeOffset? Time { get; set; }
        public decimal? Profit { get; set; }
        public decimal? ProfitPer { get; set; }
        public decimal? ExitPrice { get; set; }
        public string EntryType { get; set; }
     
    }                   


}