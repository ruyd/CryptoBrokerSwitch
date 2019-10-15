using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Piggy 
{    
    public class CoinMarketCap
    {        
        public long ID { get; set; }
       
        public string coinId { get; set; }
        public string name { get; set; }
        public string symbol { get; set; }

        public int? rank { get; set; }
        public decimal? price_usd { get; set; }
        public decimal? price_btc { get; set; }
        
        public decimal? volume_usd_24h { get; set; }
        public decimal? market_cap_usd { get; set; }

        public long? available_supply { get; set; }
        public long? total_supply { get; set; }
        public long? max_supply { get; set; }

        public decimal? percent_change_1h { get; set; }
        public decimal? percent_change_24h { get; set; }
        public decimal? percent_change_7d { get; set; }
        public DateTime? last_updated { get; set; }
        public DateTime? DateTimeCreated { get; set; }
        public int? RunID { get; set; }
    }

    public class CoinMarketCapJsonItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string symbol { get; set; }
        public string rank { get; set; }
        public string price_usd { get; set; }
        public string price_btc { get; set; }
        [JsonProperty("24h_volume_usd")]
        public string volume_usd_24h { get; set; }
        public string market_cap_usd { get; set; }
        public string available_supply { get; set; }
        public string total_supply { get; set; }
        public string max_supply { get; set; }
        public string percent_change_1h { get; set; }
        public string percent_change_24h { get; set; }
        public string percent_change_7d { get; set; }
        public string last_updated { get; set; }
    }
}
