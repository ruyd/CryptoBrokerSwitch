using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace Piggy.LocalModels 
{
    public class BinanceOrder
    {        
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long OrderId { get; set; }
 
        public string ClientOrderId { get; set; }
        public string Symbol { get; set; }        
        public decimal? Price { get; set; }
        public decimal? StopPrice { get; set; }
        public decimal? OriginalQuantity { get; set; }        
        public decimal? ExecutedQuantity { get; set; }
        public decimal? IcebergQuantity { get; set; }
        public string Status { get; set; }        
        public string TimeInForce { get; set; }        
        public string Type { get; set; }        
        public string Side { get; set; }                        
        public DateTime? Time { get; set; }        
        public bool? IsWorking { get; set; }

        public DateTime? DateTimeCreated { get; set; }
        public DateTime? DateTimeUpdated { get; set; }
    }

    public class BinanceTrade
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public long? OrderId { get; set; }
        public decimal? Price { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Commission { get; set; }
        public string CommissionAsset { get; set; }
        public DateTime? Time { get; set; }
        public bool? IsBuyer { get; set; }
        public bool? IsMaker { get; set; }
        public bool? IsBestMatch { get; set; }

        public DateTime? DateTimeCreated { get; set; }
        public DateTime? DateTimeUpdated { get; set; }
    }
}
