using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Piggy
{

    public class ScraperNofy
    {
        public string strategy { get; set; }
        public string currency { get; set; }
        public string symbol { get; set; }
        public string candle { get; set; }
        public string user { get; set; }
        public string chart { get; set; }
        public string IPAddress { get; set; }

        public List<RawTrade> trades { get; set; }

    }
    public class EntryExit
    {
        [JsonProperty("c")]
        public string Signal { get; set; }

        [JsonProperty("p")]
        public decimal? Price { get; set; }

        [JsonProperty("tm")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("tp")]
        public string TradeType { get; set; }

        public string Side => TradeType?.Contains("x") == true ? "Sell" : "Buy";

    }
    //"le": "Entry Long", "lx": "Exit Long", "se": "Entry Short", "sx": "Exit Short"


    public class RawTrade
    {
        [JsonProperty("e")]
        public EntryExit Entry { get; set; }

        [JsonProperty("pf")]
        public decimal? Profit { get; set; }

        [JsonProperty("q")]
        public decimal? Quantity { get; set; }

        [JsonProperty("x")]
        public EntryExit Exit { get; set; }

        [JsonProperty("pfp")]
        public decimal? ProfitPercent { get; set; }
    }

    public class RawOrder
    {
        [JsonProperty("b")]
        public bool? Bool1 { get; set; }

        [JsonProperty("c")]
        public string Signal { get; set; }

        [JsonProperty("e")]
        public bool? Exitish { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("p")]
        public decimal? Price { get; set; }

        [JsonProperty("q")]
        public int? Quantity { get; set; }

        [JsonProperty("tm")]
        public int? TradeNum { get; set; }

        [JsonProperty("tp")]
        public string OrderType { get; set; }
    }

}