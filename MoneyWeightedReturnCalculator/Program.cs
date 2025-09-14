using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MWR.Core.Models;
using MWR.Core.Services;

namespace MWR.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║  Money-Weighted Return Calculator (XIRR)   ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                string csvPath = GetCsvPath(args);

                if (!File.Exists(csvPath))
                {
                    Console.WriteLine($"❌ Error: File not found: {csvPath}");
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"📁 Loading data from: {Path.GetFileName(csvPath)}");
                Console.WriteLine();

                // Load data
                var dataProvider = new CsvDataProvider();
                var transactions = await dataProvider.GetTransactionsAsync(csvPath);
                var portfolio = await dataProvider.GetCurrentPortfolioValueAsync(csvPath);

                // Display current portfolio summary
                DisplayPortfolioSummary(portfolio);

                // Calculate returns
                var calculator = new MwrCalculator();
                var results = calculator.CalculateReturns(transactions, portfolio.TotalValue);

                // Display results
                DisplayPerformanceResults(results);

                // Export option
                Console.WriteLine("\n💾 Would you like to export results to CSV? (y/n): ");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    await ExportResults(results, portfolio);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine("\nStack Trace:");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }

        private static string GetCsvPath(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                return args[0];
            }

            Console.WriteLine("Enter the path to your CSV file:");
            Console.WriteLine("(or drag and drop the file here)");
            Console.Write("> ");

            string path = Console.ReadLine()?.Trim() ?? "";

            // Remove quotes if present (from drag and drop)
            if (path.StartsWith("\"") && path.EndsWith("\""))
            {
                path = path.Substring(1, path.Length - 2);
            }

            return path;
        }

        private static void DisplayPortfolioSummary(PortfolioSnapshot portfolio)
        {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("📊 CURRENT PORTFOLIO SUMMARY");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine($"Valuation Date: {portfolio.Date:yyyy-MM-dd}");
            Console.WriteLine($"Total Value: {portfolio.TotalValue:C}");
            Console.WriteLine($"Positions: {portfolio.Positions.Count}");
            Console.WriteLine();

            if (portfolio.Positions.Any())
            {
                Console.WriteLine("Top Holdings:");
                var topHoldings = portfolio.Positions.Values
                    .OrderByDescending(p => p.MarketValue)
                    .Take(5);

                foreach (var position in topHoldings)
                {
                    var allocation = (position.MarketValue / portfolio.TotalValue) * 100;
                    var gainLossIndicator = position.UnrealizedGainLoss >= 0 ? "▲" : "▼";
                    var gainLossColor = position.UnrealizedGainLoss >= 0 ? ConsoleColor.Green : ConsoleColor.Red;

                    Console.Write($"  {position.Symbol,-6} ");
                    Console.Write($"{position.MarketValue,12:C} ");
                    Console.Write($"({allocation,5:F1}%) ");

                    Console.ForegroundColor = gainLossColor;
                    Console.WriteLine($"{gainLossIndicator} {position.UnrealizedGainLossPercent,6:F2}%");
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        private static void DisplayPerformanceResults(List<PerformanceResult> results)
        {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("📈 MONEY-WEIGHTED RETURNS (XIRR)");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("Period        MWR     Net Gain/Loss    Contributions");
            Console.WriteLine("─────────────────────────────────────────────────────");

            foreach (var result in results)
            {
                var returnColor = result.MoneyWeightedReturn >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                var gainLossColor = result.NetGainLoss >= 0 ? ConsoleColor.Green : ConsoleColor.Red;

                Console.Write($"{result.Period,-10} ");

                Console.ForegroundColor = returnColor;
                Console.Write($"{result.MoneyWeightedReturn,7:F2}%");
                Console.ResetColor();

                Console.Write("  ");

                Console.ForegroundColor = gainLossColor;
                Console.Write($"{result.NetGainLoss,13:C}");
                Console.ResetColor();

                Console.WriteLine($"  {result.TotalContributions,13:C}");
            }

            Console.WriteLine();
            Console.WriteLine("📝 Note: Returns are annualized using the XIRR method");
        }

        private static async Task ExportResults(List<PerformanceResult> results, PortfolioSnapshot portfolio)
        {
            try
            {
                var fileName = $"MWR_Results_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var outputPath = Path.Combine(Environment.CurrentDirectory, fileName);

                using var writer = new StreamWriter(outputPath);

                // Write header
                await writer.WriteLineAsync("Period,Start Date,End Date,MWR %,Total Contributions,Total Withdrawals,Ending Value,Net Gain/Loss");

                // Write data
                foreach (var result in results)
                {
                    await writer.WriteLineAsync($"{result.Period}," +
                        $"{result.StartDate:yyyy-MM-dd}," +
                        $"{result.EndDate:yyyy-MM-dd}," +
                        $"{result.MoneyWeightedReturn:F2}," +
                        $"{result.TotalContributions:F2}," +
                        $"{result.TotalWithdrawals:F2}," +
                        $"{result.EndingValue:F2}," +
                        $"{result.NetGainLoss:F2}");
                }

                Console.WriteLine($"\n✅ Results exported to: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Export failed: {ex.Message}");
            }
        }
    }
}