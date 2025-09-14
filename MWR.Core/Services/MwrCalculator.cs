using System;
using System.Collections.Generic;
using System.Linq;
using MWR.Core.Models;

namespace MWR.Core.Services
{
    public class MwrCalculator
    {
        private const double Tolerance = 0.00001;
        private const int MaxIterations = 100;

        public List<PerformanceResult> CalculateReturns(
            List<Transaction> transactions,
            decimal currentPortfolioValue,
            DateTime? asOfDate = null)
        {
            var results = new List<PerformanceResult>();
            var evaluationDate = asOfDate ?? DateTime.Now;

            // Define periods to calculate
            var periods = new[]
            {
                ("YTD", new DateTime(evaluationDate.Year, 1, 1)),
                ("1 Year", evaluationDate.AddYears(-1)),
                ("2 Years", evaluationDate.AddYears(-2)),
                ("3 Years", evaluationDate.AddYears(-3)),
                ("4 Years", evaluationDate.AddYears(-4)),
                ("5 Years", evaluationDate.AddYears(-5)),
                ("All Time", transactions.Min(t => t.Date))
            };

            foreach (var (periodName, startDate) in periods)
            {
                if (startDate > evaluationDate) continue;

                var periodTransactions = transactions
                    .Where(t => t.Date >= startDate && t.Date <= evaluationDate)
                    .OrderBy(t => t.Date)
                    .ToList();

                if (!periodTransactions.Any()) continue;

                var result = CalculatePeriodReturn(
                    periodTransactions,
                    currentPortfolioValue,
                    startDate,
                    evaluationDate,
                    periodName);

                results.Add(result);
            }

            return results;
        }

        private PerformanceResult CalculatePeriodReturn(
            List<Transaction> transactions,
            decimal currentValue,
            DateTime startDate,
            DateTime endDate,
            string periodName)
        {
            // Prepare cash flows for XIRR calculation
            var cashFlows = new List<(DateTime date, double amount)>();

            // Add all transaction cash flows
            foreach (var transaction in transactions)
            {
                if (transaction.CashFlow != 0)
                {
                    cashFlows.Add((transaction.Date, (double)transaction.CashFlow));
                }
            }

            // Add the current portfolio value as a positive cash flow (as if we're selling everything)
            cashFlows.Add((endDate, (double)currentValue));

            // Calculate XIRR
            double xirr = CalculateXIRR(cashFlows);

            // Calculate summary statistics
            var contributions = transactions
                .Where(t => t.Type == TransactionType.Deposit || t.Type == TransactionType.Buy)
                .Sum(t => Math.Abs(t.CashFlow));

            var withdrawals = transactions
                .Where(t => t.Type == TransactionType.Withdrawal || t.Type == TransactionType.Sell || t.Type == TransactionType.Dividend)
                .Sum(t => t.CashFlow);

            return new PerformanceResult
            {
                Period = periodName,
                StartDate = startDate,
                EndDate = endDate,
                MoneyWeightedReturn = (decimal)(xirr * 100), // Convert to percentage
                TotalContributions = contributions,
                TotalWithdrawals = withdrawals,
                EndingValue = currentValue,
                StartingValue = 0 // Could be calculated if we track historical values
            };
        }

        private double CalculateXIRR(List<(DateTime date, double amount)> cashFlows)
        {
            if (cashFlows.Count < 2)
                return 0;

            // Sort cash flows by date
            cashFlows = cashFlows.OrderBy(cf => cf.date).ToList();

            var firstDate = cashFlows[0].date;

            // Convert dates to years from first date
            var flows = cashFlows.Select(cf => new
            {
                Years = (cf.date - firstDate).TotalDays / 365.25,
                Amount = cf.amount
            }).ToList();

            // Initial guess for rate
            double rate = 0.1;

            // Newton-Raphson method to find IRR
            for (int i = 0; i < MaxIterations; i++)
            {
                double npv = 0;
                double dnpv = 0;

                foreach (var flow in flows)
                {
                    double pv = flow.Amount / Math.Pow(1 + rate, flow.Years);
                    npv += pv;
                    dnpv -= flow.Years * pv / (1 + rate);
                }

                if (Math.Abs(npv) < Tolerance)
                    return rate;

                if (Math.Abs(dnpv) < Tolerance)
                    break;

                rate = rate - npv / dnpv;

                // Bound the rate to prevent overflow
                if (rate < -0.99)
                    rate = -0.99;
                else if (rate > 10)
                    rate = 10;
            }

            return rate;
        }

        public decimal CalculateTimeWeightedReturn(
            List<Transaction> transactions,
            List<(DateTime date, decimal value)> portfolioValues)
        {
            // TWR implementation for future comparison
            // This would require daily/periodic portfolio valuations
            if (portfolioValues.Count < 2)
                return 0;

            decimal totalReturn = 1;

            for (int i = 1; i < portfolioValues.Count; i++)
            {
                var startValue = portfolioValues[i - 1].value;
                var endValue = portfolioValues[i].value;

                // Get cash flows between these dates
                var cashFlows = transactions
                    .Where(t => t.Date > portfolioValues[i - 1].date &&
                               t.Date <= portfolioValues[i].date)
                    .Sum(t => t.CashFlow);

                if (startValue != 0)
                {
                    var periodReturn = (endValue + cashFlows) / startValue;
                    totalReturn *= periodReturn;
                }
            }

            return (totalReturn - 1) * 100; // Return as percentage
        }
    }
}