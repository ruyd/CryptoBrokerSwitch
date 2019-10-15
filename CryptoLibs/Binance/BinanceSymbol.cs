using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy 
{
    public class BinanceSymbol
    {
        public int ID { get; set; } 
        public string Symbol { get; set; }        
        public string Status { get; set; }
        public string BaseAsset { get; set; }        
        public string QuoteAsset { get; set; } 
        public DateTime? DateTimeCreated { get; set; }
        public DateTime? DateTimeUpdated { get; set; }
        
        
    }
}
