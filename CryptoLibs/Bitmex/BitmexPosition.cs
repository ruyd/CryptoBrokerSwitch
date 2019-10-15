using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy
{
    public class BitmexPosition
    {
        public string Side => currentQty >= 0 ? "Buy" : "Sell";
        public string Signal => Side == "Buy" ? "long" : "short";
        public decimal? PositiveQuantity => currentQty >= 0 ? currentQty : currentQty * -1;

        public decimal? account { get; set; }
        public string symbol { get; set; }
        public string currency { get; set; }
        public string underlying { get; set; }
        public string quoteCurrency { get; set; }
        public decimal? commission { get; set; }
        public decimal? initMarginReq { get; set; }
        public decimal? maintMarginReq { get; set; }
        public decimal? riskLimit { get; set; }
        public int? leverage { get; set; }
        public bool? crossMargin { get; set; }
        public decimal? deleveragePercentile { get; set; }
        public decimal? rebalancedPnl { get; set; }
        public decimal? prevRealisedPnl { get; set; }
        public decimal? prevUnrealisedPnl { get; set; }
        public decimal? prevClosePrice { get; set; }
        public DateTime? openingTimestamp { get; set; }
        public decimal? openingQty { get; set; }
        public decimal? openingCost { get; set; }
        public decimal? openingComm { get; set; }
        public decimal? openOrderBuyQty { get; set; }
        public decimal? openOrderBuyCost { get; set; }
        public decimal? openOrderBuyPremium { get; set; }
        public decimal? openOrderSellQty { get; set; }
        public decimal? openOrderSellCost { get; set; }
        public decimal? openOrderSellPremium { get; set; }
        public decimal? execBuyQty { get; set; }
        public decimal? execBuyCost { get; set; }
        public decimal? execSellQty { get; set; }
        public decimal? execSellCost { get; set; }
        public decimal? execQty { get; set; }
        public decimal? execCost { get; set; }
        public decimal? execComm { get; set; }
        public DateTime? currentTimestamp { get; set; }
        public decimal? currentQty { get; set; }
        public decimal? currentCost { get; set; }
        public decimal? currentComm { get; set; }
        public decimal? realisedCost { get; set; }
        public decimal? unrealisedCost { get; set; }
        public decimal? grossOpenCost { get; set; }
        public decimal? grossOpenPremium { get; set; }
        public decimal? grossExecCost { get; set; }
        public bool? isOpen { get; set; }
        public decimal? markPrice { get; set; }
        public decimal? markValue { get; set; }
        public decimal? riskValue { get; set; }
        public decimal? homeNotional { get; set; }
        public decimal? foreignNotional { get; set; }
        public string posState { get; set; }
        public decimal? posCost { get; set; }
        public decimal? posCost2 { get; set; }
        public decimal? posCross { get; set; }
        public decimal? posInit { get; set; }
        public decimal? posComm { get; set; }
        public decimal? posLoss { get; set; }
        public decimal? posMargin { get; set; }
        public decimal? posMaint { get; set; }
        public decimal? posAllowance { get; set; }
        public decimal? taxableMargin { get; set; }
        public decimal? initMargin { get; set; }
        public decimal? maintMargin { get; set; }
        public decimal? sessionMargin { get; set; }
        public decimal? targetExcessMargin { get; set; }
        public decimal? varMargin { get; set; }
        public decimal? realisedGrossPnl { get; set; }
        public decimal? realisedTax { get; set; }
        public decimal? realisedPnl { get; set; }
        public decimal? unrealisedGrossPnl { get; set; }
        public decimal? longBankrupt { get; set; }
        public decimal? shortBankrupt { get; set; }
        public decimal? taxBase { get; set; }
        public decimal? indicativeTaxRate { get; set; }
        public decimal? indicativeTax { get; set; }
        public decimal? unrealisedTax { get; set; }
        public decimal? unrealisedPnl { get; set; }
        public decimal? unrealisedPnlPcnt { get; set; }
        public decimal? unrealisedRoePcnt { get; set; }
        public decimal? simpleQty { get; set; }
        public decimal? simpleCost { get; set; }
        public decimal? simpleValue { get; set; }
        public decimal? simplePnl { get; set; }
        public decimal? simplePnlPcnt { get; set; }
        public decimal? avgCostPrice { get; set; }
        public decimal? avgEntryPrice { get; set; }
        public decimal? breakEvenPrice { get; set; }
        public decimal? marginCallPrice { get; set; }
        public decimal? liquidationPrice { get; set; }
        public decimal? bankruptPrice { get; set; }
        public DateTime? timestamp { get; set; }
        public decimal? lastPrice { get; set; }
        public decimal? lastValue { get; set; }
        public double? AgeInSeconds => (DateTime.UtcNow - timestamp)?.TotalSeconds;
    }

}
