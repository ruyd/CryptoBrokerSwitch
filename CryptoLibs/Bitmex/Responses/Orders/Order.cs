using System;
using System.Diagnostics;
using Piggy;

namespace Bitmex 
{
    [DebuggerDisplay("Exec: {ExecId}, {LastQty}. {LastPx}")]
    public class OrderExecution : Order
    {
        public string execId { get; set; }
        public string lastQty { get; set; }
        public string lastPx { get; set; }
        public string underlyingLastPx { get; set; }
        public string trdMatchID { get; set; }
        public string commission { get; set; }
        public string execCost { get; set; }
        public string execComm { get; set; }
        
    }


    [DebuggerDisplay("Order: {Symbol}, {OrderQty}. {Price}")]
    public class Order
    {
        
        public string orderId { get; set; }
        public string clOrdId { get; set; }
        public string clOrdLinkId {get; set; }

        public long? account { get; set; }
        public string symbol { get; set; }
        public string side { get; set; }

        public decimal? simpleOrderQty { get; set; }
        public long? orderQty {get; set; }

        public decimal? price { get; set; }

        public long? displayQty { get; set; }
        public decimal? stopPx { get; set; }

        public decimal? pegOffsetValue { get; set; }
        public string pegPriceType { get; set; }

        public string currency { get; set; }
        public string settlCurrency { get; set; }

        public string ordType { get; set; }
        public string timeInForce { get; set; }
        public string execInst { get; set; }
        public string contingencyType { get; set; }
        public string exDestination { get; set; }
        public string ordStatus { get; set; }
        public string triggered { get; set; }

        public bool? workingIndicator { get; set; }
        public string ordRejReason { get; set; }
        public decimal? simpleLeavesQty { get; set; }
        public long? leavesQty { get; set; }
        public decimal? simpleCumQty { get; set; }
        public long? cumQty { get; set; }
        public decimal? avgPx { get; set; }
        public string multiLegReportingType { get; set; }
        public string text {get; set; }

        public  BitmexError error { get; set; }

        public DateTime? transactTime { get; set; }
        public DateTime? timestamp { get; set; }

    }
}
