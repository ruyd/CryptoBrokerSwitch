using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy
{
    public static partial class BrokerModeling
    {
        public static BrokerStrategy ToStrategy(this BrokerPreference p)
        {
            var s = new BrokerStrategy();

            s.FK_UserID = p.FK_UserID;

            s.EnableLong = p.EnableLong;
            s.EnableShort = p.EnableShort;
            s.Quantity = p.Quantity;
            s.EnableMarket = p.EnableMarket;
            s.LimitOptionId = p.LimitOptionId;
            s.EnableAutoClose = p.EnableAutoClose;
            s.EnablePush = p.EnablePush;
            s.EnableEmail = p.EnableEmail;

            s.DateTimeCreated = DateTime.UtcNow;

            return s;
        }

        public static BrokerStrategiesRun ToRun(this BrokerStrategy s)
        {
            var r = new BrokerStrategiesRun();

            r.FK_UserID = s.FK_UserID;

            r.FK_StrategyID = s.ID;
            r.DateTimeStarted = DateTime.UtcNow;
            r.StartStrategyName = s.StrategyName;
            r.StartChartId = s.ChartId;
            r.StartInterval = s.CandleInterval;
            r.StartQuantity = s.Quantity;

            return r;
        }

     

        public static BrokerStrategiesTrade ToEntryTrade(this OrderResponse t)
        {
            var x = new BrokerStrategiesTrade();
            
            x.UniqueID = Guid.NewGuid();
            x.DateTimeCreated = DateTime.UtcNow;
            
            x.TradeNum = 0;
            x.StatusID = 1;
            x.Symbol = t.symbol;
            x.EntryAsyncFillTime = DateTime.UtcNow;
            x.SourceName = "MANUAL";
            x.EntryFee = t.commission;

            x.AskQuantity = t.orderQty;
            x.AskPrice = t.price;
            x.AskType = t.ordType;

            x.EntryTime = DateTime.UtcNow;
            x.EntrySignal = t.side == "Buy" ? "long" : "short";
            x.EntryType = t.ordType;
            x.EntryPrice = t.price;
            x.EntryQuantity = t.orderQty;
            x.EntryMessage = t.ordStatus;

            x.Success = true; 
            
            return x;
        }

        public static BrokerStrategy ToStrategy(this NewOrderPost req)
        {
            var s = new BrokerStrategy();
            s.DateTimeCreated = DateTime.UtcNow;
            s.DateTimeUpdated = DateTime.UtcNow;

            var x = req.Data;

            s.FK_UserID = req.ID;
            s.StrategyName = x.strategyName;

            var sym = x.symbol.Split(':');
            s.Exchange = sym.FirstOrDefault();
            s.Symbol = sym.LastOrDefault();
            s.ChartId = x.chart;
            s.CandleInterval = x.candle;
            s.StrategyId = x.strategyId;
            s.ChromeTabId = x.tabId;

            return s;
        }

        public static BrokerStrategiesTrade ToTrade(this NewOrderPost request, RawOrder item)
        {
            var x = new BrokerStrategiesTrade();

            var nofy = request.Data; 
            x.UniqueID = Guid.NewGuid();
            x.DateTimeCreated = DateTime.UtcNow;

            x.TradeNum = nofy.orders?.FirstOrDefault()?.TradeNum ?? nofy.orders?.IndexOf(item) + 1;
            
            x.AskQuantity = item.Quantity;
            x.AskPrice = item.Price;
            x.AskType = item.OrderType;

            x.EntryTime = DateTime.UtcNow;
            x.EntrySignal = item.Signal;
            x.EntryType = item.OrderType.ToCamelCase();
 
            return x;
        }
    }
}
