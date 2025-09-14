using CsvHelper;
using CsvHelper.Configuration;
using MWR.Core.Models;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MWR.Core.Services
{
    public class CsvDataProvider : IDataProvider
    {
        public async Task<List<Transaction>> GetTransactionsAsync(string csvFilePath)
        {
            var transactions = new List<Transaction>();

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            var records = csv.GetRecords<CsvTransaction>();

            foreach (var record in records)
            {
                transactions.Add(MapToTransaction(record));
            }

            return await Task.FromResult(transactions.OrderBy(t => t.Date).ToList());
        }

        public async Task<PortfolioSnapshot> GetCurrentPortfolioValueAsync(string csvFilePath)
        {
            // This could read from a separate positions file or calculate from transactions
            // For now, we'll calculate from the transactions
            var transactions = await GetTransactionsAsync(csvFilePath);
            var snapshot = new PortfolioSnapshot
            {
                Date = DateTime.Now,
                Positions = new Dictionary<string, PositionDetail>()
            };

            // Calculate current positions from transaction history
            var positions = transactions
                .Where(t => t.Type == TransactionType.Buy || t.Type == TransactionType.Sell)
                .GroupBy(t => t.Symbol)
                .Select(g => new
                {
                    Symbol = g.Key,
                    NetShares = g.Sum(t => t.Type == TransactionType.Buy ? t.Shares : -t.Shares),
                    CostBasis = g.Sum(t => t.Type == TransactionType.Buy ? t.TotalAmount : -t.TotalAmount),
                    CurrentPrice = g.OrderByDescending(t => t.Date).First().CurrentPrice
                })
                .Where(p => p.NetShares > 0);

            foreach (var pos in positions)
            {
                snapshot.Positions[pos.Symbol] = new PositionDetail
                {
                    Symbol = pos.Symbol,
                    Shares = pos.NetShares,
                    CurrentPrice = pos.CurrentPrice,
                    CostBasis = pos.CostBasis
                };
            }

            snapshot.TotalValue = snapshot.Positions.Values.Sum(p => p.MarketValue);
            return snapshot;
        }

        private Transaction MapToTransaction(CsvTransaction csv)
        {
            return new Transaction
            {
                Date = DateTime.Parse(csv.Date),
                Type = Enum.Parse<TransactionType>(csv.Type, true),
                Symbol = csv.Symbol,
                Shares = csv.Shares ?? 0,
                PricePerShare = csv.PricePerShare ?? 0,
                CurrentPrice = csv.CurrentPrice ?? 0,
                CashAmount = csv.CashAmount ?? 0
            };
        }
    }

    // CSV mapping class
    public class CsvTransaction
    {
        public string Date { get; set; }
        public string Type { get; set; }
        public string Symbol { get; set; }
        public decimal? Shares { get; set; }
        public decimal? PricePerShare { get; set; }
        public decimal? CurrentPrice { get; set; }
        public decimal? CashAmount { get; set; }
    }
}