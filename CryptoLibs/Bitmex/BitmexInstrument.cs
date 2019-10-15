using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy
{
    public class BitmexInstrument
    {
        public int? ID { get; set; }
        public string symbol { get; set; }
        public string rootSymbol { get; set; }
        public string state { get; set; }

        public long? prevTotalVolume { get; set; }
        public long? totalVolume { get; set; }
        public long? volume { get; set; }
        public long? volume24h { get; set; }

        public decimal? prevClosePrice { get; set; }
        public decimal? prevPrice24h { get; set; }
        public decimal? vwap { get; set; }
        public decimal? highPrice { get; set; }
        public decimal? lowPrice { get; set; }
        public decimal? lastPrice { get; set; }
        public decimal? lastPriceProtected { get; set; }
        public string lastTickDirection { get; set; }
        public decimal? lastChangePcnt { get; set; }
        public decimal? bidPrice { get; set; }
        public decimal? midPrice { get; set; }
        public decimal? askPrice { get; set; }
        public decimal? impactBidPrice { get; set; }
        public decimal? impactMidPrice { get; set; }
        public decimal? impactAskPrice { get; set; }
        public string markMethod { get; set; }
        public decimal? markPrice { get; set; }

        public decimal? fundingRate { get; set; }
        public decimal? indicativeFundingRate { get; set; }
        public decimal? fairBasisRate { get; set; }

        public DateTimeOffset? openingTimestamp { get; set; }
        public DateTimeOffset? closingTimestamp { get; set; }
        public DateTimeOffset? timestamp { get; set; }
        public long? BuySize { get; set; }
        public long? SellSize { get; set; }
        public DateTime? DateTimeCreated { get; set; }
    }
}
