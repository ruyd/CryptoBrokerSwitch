using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy 
{
    public static class BinanceModeling
    {
        public static LocalModels.BinanceOrder ToDbOrder(this Binance.Net.Objects.BinanceOrder o)
        {
            var a = new LocalModels.BinanceOrder();
            a.DateTimeCreated = DateTime.Now;
            a.DateTimeUpdated = DateTime.Now;
            o.CopyPropertiesTo(a);
            return a;
        }

        public static void CopyPropertiesTo(this Binance.Net.Objects.BinanceOrder o, LocalModels.BinanceOrder target)
        {
            target.OrderId = o.OrderId;
            target.ClientOrderId = o.ClientOrderId;

            target.ExecutedQuantity = o.ExecutedQuantity;
            target.IcebergQuantity = o.IcebergQuantity;
            target.OriginalQuantity = o.OriginalQuantity;

            target.IsWorking = o.IsWorking;

            target.Price = o.Price;
            target.StopPrice = o.StopPrice;

            target.Side = o.Side.ToString();
            target.Status = o.Status.ToString();
            target.TimeInForce = o.TimeInForce.ToString();
            target.Type = o.Type.ToString();

            target.Symbol = o.Symbol;
            target.Time = o.Time;
        }

        public static LocalModels.BinanceTrade ToDbTrade(this Binance.Net.Objects.BinanceTrade o)
        {
            var a = new LocalModels.BinanceTrade();
            a.DateTimeCreated = DateTime.Now;
            a.DateTimeUpdated = DateTime.Now;
            o.CopyPropertiesTo(a);          
            return a;
        }

        public static void CopyPropertiesTo(this Binance.Net.Objects.BinanceTrade o, LocalModels.BinanceTrade target)
        {
 
            target.Id = o.Id;
  
            target.Commission = o.Commission;
            target.CommissionAsset = o.CommissionAsset;

            target.IsBestMatch = o.IsBestMatch;
            target.IsBuyer = o.IsBuyer;
            target.IsMaker = o.IsMaker;
            target.OrderId = o.OrderId;
            target.Price = o.Price;
            target.Quantity = o.Quantity;
            target.Time = o.Time;

        }
    }
}
