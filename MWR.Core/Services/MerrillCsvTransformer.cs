using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace MWR.Core.Services
{
    public class MerrillCsvTransformer
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task TransformMerrillCsv(string inputPath, string outputPath)
        {
            var merrillTransactions = ReadMerrillCsv(inputPath);
            var transformedTransactions = new List<TransformedTransaction>();

            Console.WriteLine($"Processing {merrillTransactions.Count} transactions...");

            // Group by symbol to fetch current prices efficiently
            var symbols = merrillTransactions
                .Where(t => !string.IsNullOrEmpty(t.Symbol))
                .Select(t => t.Symbol)
                .Distinct()
                .ToList();

            var currentPrices = new Dictionary<string, decimal>();

            // Fetch current prices (you'd need to implement this with a real API)
            foreach (var symbol in symbols)
            {
                Console.WriteLine($"Fetching current price for {symbol}...");
                // For demo purposes, using placeholder
                // In reality, you'd call an API like Yahoo Finance or Alpha Vantage
                currentPrices[symbol] = await GetCurrentPrice(symbol);
            }

            foreach (var merrill in merrillTransactions)
            {
                var transformed = new TransformedTransaction
                {
                    Date = merrill.TradeDate ?? merrill.SettlementDate ?? DateTime.Now,
                    Type = MapTransactionType(merrill.TransactionType),
                    Symbol = merrill.Symbol,
                    Shares = Math.Abs(merrill.Quantity ?? 0),
                    PricePerShare = Math.Abs(merrill.Price ?? 0),
                    CurrentPrice = !string.IsNullOrEmpty(merrill.Symbol) && currentPrices.ContainsKey(merrill.Symbol)
                        ? currentPrices[merrill.Symbol]
                        : 0,
                    CashAmount = 0
                };

                // Handle cash transactions
                if (transformed.Type == "Deposit" || transformed.Type == "Withdrawal")
                {
                    transformed.CashAmount = Math.Abs(merrill.Amount ?? 0);
                    transformed.Shares = 0;
                    transformed.PricePerShare = 0;
                }
                else if (transformed.Type == "Dividend")
                {
                    transformed.CashAmount = Math.Abs(merrill.Amount ?? 0);
                }

                transformedTransactions.Add(transformed);
            }

            // Write output CSV
            WriteTranformedCsv(outputPath, transformedTransactions);
            Console.WriteLine($"✅ Transformation complete! Output saved to: {outputPath}");
        }

        private List<MerrillTransaction> ReadMerrillCsv(string path)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                PrepareHeaderForMatch = args => args.Header.ToLower().Replace(" ", "").Replace("_", "")
            });

            return csv.GetRecords<MerrillTransaction>().ToList();
        }

        private void WriteTranformedCsv(string path, List<TransformedTransaction> transactions)
        {
            using var writer = new StreamWriter(path);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(transactions);
        }

        private string MapTransactionType(string merrillType)
        {
            if (string.IsNullOrEmpty(merrillType))
                return "Unknown";

            var type = merrillType.ToLower();

            return type switch
            {
                var t when t.Contains("bought") || t.Contains("buy") => "Buy",
                var t when t.Contains("sold") || t.Contains("sell") => "Sell",
                var t when t.Contains("dividend") || t.Contains("div") => "Dividend",
                var t when t.Contains("deposit") || t.Contains("contribution") => "Deposit",
                var t when t.Contains("withdrawal") || t.Contains("distribution") => "Withdrawal",
                var t when t.Contains("interest") => "Dividend",
                _ => type
            };
        }

        private async Task<decimal> GetCurrentPrice(string symbol)
        {
            // Placeholder - in production, you'd use a real API
            // Options:
            // 1. Yahoo Finance API (unofficial)
            // 2. Alpha Vantage (free tier available)
            // 3. IEX Cloud (free tier available)
            // 4. Twelve Data (free tier available)

            // For now, return a placeholder
            await Task.Delay(100); // Simulate API call

            // You could manually maintain a dictionary for testing:
            var manualPrices = new Dictionary<string, decimal>
            {
                ["AAPL"] = 180.95m,
                ["MSFT"] = 420.55m,
                ["GOOGL"] = 173.49m,
                ["TSLA"] = 178.87m,
                ["NVDA"] = 881.86m,
                ["AMZN"] = 178.86m,
                ["META"] = 514.87m
            };

            return manualPrices.ContainsKey(symbol) ? manualPrices[symbol] : 100m;
        }
    }

    // Merrill Edge CSV format (adjust field names based on actual export)
    public class MerrillTransaction
    {
        public DateTime? TradeDate { get; set; }
        public DateTime? SettlementDate { get; set; }
        public string TransactionType { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Commission { get; set; }
    }

    // Our format
    public class TransformedTransaction
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Symbol { get; set; }
        public decimal Shares { get; set; }
        public decimal PricePerShare { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal CashAmount { get; set; }
    }
}