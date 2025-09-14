using System;
using System.Collections.Generic;

namespace MWR.Core.Models
{
    public class PortfolioSnapshot
    {
        public DateTime Date { get; set; }
        public decimal TotalValue { get; set; }
        public Dictionary<string, PositionDetail> Positions { get; set; } = new();
    }

    public class PositionDetail
    {
        public string Symbol { get; set; }
        public decimal Shares { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal MarketValue => Shares * CurrentPrice;
        public decimal CostBasis { get; set; }
        public decimal UnrealizedGainLoss => MarketValue - CostBasis;
        public decimal UnrealizedGainLossPercent => CostBasis != 0 ? (UnrealizedGainLoss / CostBasis) * 100 : 0;
    }

    public class PerformanceResult
    {
        public string Period { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal MoneyWeightedReturn { get; set; }
        public decimal TotalContributions { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal StartingValue { get; set; }
        public decimal EndingValue { get; set; }
        public decimal NetGainLoss => EndingValue - StartingValue - TotalContributions + TotalWithdrawals;
    }
}