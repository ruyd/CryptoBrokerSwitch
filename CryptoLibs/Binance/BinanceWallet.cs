using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy 
{
    public class BinanceWallet
    {        
        public int ID { get; set; }
        public string Asset { get; set; }
        public decimal? Free { get; set; }
        public decimal? Locked { get; set; }
        public decimal? Total { get; set; }
        public string LastBuySymbol { get; set; }        
        public decimal? LastBuyPrice { get; set; }
        public DateTime? LastBuyDateTime { get; set; }
        public decimal? FirstBuyPrice { get; set; }
        public DateTime? FirstBuyDateTime { get; set; }
        public DateTime? DateTimeCreated { get; set; }
        public DateTime? DateTimeUpdated { get; set; }
    }
}
