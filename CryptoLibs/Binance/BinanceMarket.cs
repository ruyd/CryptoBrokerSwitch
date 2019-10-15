using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Piggy 
{

    public class MarketVolume
    {
        public DateTime? Date { get; set; }
        public decimal? QuoteVolume { get; set; }
        public decimal? Volume { get; set; }
    }

    public class BinanceMarket
    {
        public long ID { get; set; }
        public DateTime? DateTimeCreated { get; set; }
        public int? RunID { get; set; }

        public string Symbol { get; set; }
        public decimal? PriceChange { get; set; }
        public decimal? PriceChangePercent { get; set; }
        public decimal? WeightedAveragePrice { get; set; }
        public decimal? PreviousClosePrice { get; set; }
        public decimal? LastPrice { get; set; }
        public decimal? LastQuantity { get; set; }
        public decimal? BidPrice { get; set; }
        public decimal? BidQuantity { get; set; }
        public decimal? AskPrice { get; set; }
        public decimal? AskQuantity { get; set; }
        public decimal? OpenPrice { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? LowPrice { get; set; }
        public decimal? Volume { get; set; }
        public decimal? QuoteVolume { get; set; }
        public DateTime? OpenTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public long? FirstId { get; set; }
        public long? LastId { get; set; }
        public int? Trades { get; set; }
    }

}