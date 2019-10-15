using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy 
{
    public class CoinSetting
    {
        public int ID { get; set; }
        public string Symbol { get; set; }

        public bool? Star { get; set; }
        public bool? Mute { get; set; }    
        public decimal? AlertHighPrice { get; set; }
        public decimal? AlertLowPrice { get; set; }        

        public bool? AutoTrade { get; set; }
        public decimal? AutoBuyPrice { get; set; }
        public decimal? AutoSellPrice { get; set; }
        public decimal? AllowStopCreep { get; set; }
        public decimal? AllowYoloMode { get; set; }
        public decimal? ScalpMinProfit { get; set; }
        public decimal? ScalpPercentage { get; set; }        

        public DateTime? DateTimeCreated { get; set; }
        public DateTime? DateTimeUpdated { get; set; }

        [NotMapped]
        public decimal? LastPrice { get; set; }
        [NotMapped]
        public decimal? PriceChange { get; set; }
        [NotMapped]
        public decimal? PriceChangePercent { get; set; }
        [NotMapped]
        public decimal? PreviousClosePrice { get; set; }
        [NotMapped]
        public decimal? OpenPrice { get; set; }
        [NotMapped]
        public decimal? HighPrice { get; set; }
        [NotMapped]
        public decimal? LowPrice { get; set; }
        [NotMapped]
        public decimal? QuoteVolume { get; set; }
        [NotMapped]
        public decimal? RSI { get; set; }

    }
}
