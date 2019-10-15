using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy 
{
    public class CoinIndicator
    {
        public string Symbol { get; set; }
        public decimal? PriceChange { get; set; }
        public decimal? PriceChangePercent { get; set; }
        public decimal? PreviousClosePrice { get; set; }
        public decimal? LastPrice { get; set; }
        public decimal? OpenPrice { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? LowPrice { get; set; }
        public decimal? WeightedAveragePrice { get; set; }
        public decimal? Volume { get; set; }
        public decimal? QuoteVolume { get; set; }
        public DateTime? OpenTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public int? Trades { get; set; }
        public decimal? RS { get; set; }
        public decimal? RSI { get; set; }
        public decimal? OBV { get; set; }
    }

    public class CoinWinner
    {
        public string Asset { get; set; }
        public string Symbol { get; set; }

        public decimal? Total { get; set; }
        public decimal? LastBuyPrice { get; set; }
        public decimal? LastPrice { get; set; }
        public decimal? PriceChangePercent { get; set; }
        public decimal? PriceChange { get; set; }
 
        public decimal? BuyPositionCost { get; set; }
        public decimal? BuyPositionCostUSDT { get; set; }

        public decimal? SaleGross { get; set; }
        public decimal? SaleGrossUSDT { get; set; }
        
        public decimal? SaleProfitPercentage { get; set; }        
        public decimal? SaleProfitUSDT { get; set; }
        public decimal? SaleProfit { get; set; }

        public decimal? QuoteVolume { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? LowPrice { get; set; }
        public int? Trades { get; set; }
        public decimal? BidPrice { get; set; }
        public decimal? BidQuantity { get; set; }
        public decimal? AskQuantity { get; set; }
        public decimal? AskPrice { get; set; }

        public decimal? StopPrice { get; set; }
        public long? StopOrderId { get; set; }

    }
}
