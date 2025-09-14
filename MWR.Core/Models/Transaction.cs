using System;

namespace MWR.Core.Models
{
    public class Transaction
    {
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public string Symbol { get; set; }
        public decimal Shares { get; set; }
        public decimal PricePerShare { get; set; }
        public decimal TotalAmount => Shares * PricePerShare;
        public decimal CurrentPrice { get; set; }
        public decimal CurrentValue => Shares * CurrentPrice;

        // For cash flows (deposits/withdrawals)
        public decimal CashAmount { get; set; }

        // Computed property to get cash flow value for XIRR calculation
        public decimal CashFlow
        {
            get
            {
                return Type switch
                {
                    TransactionType.Deposit => -CashAmount,  // Money going in (negative)
                    TransactionType.Withdrawal => CashAmount, // Money coming out (positive)
                    TransactionType.Buy => -TotalAmount,      // Money going in
                    TransactionType.Sell => TotalAmount,      // Money coming out
                    TransactionType.Dividend => CashAmount,   // Money coming out
                    _ => 0
                };
            }
        }
    }

    public enum TransactionType
    {
        Buy,
        Sell,
        Deposit,
        Withdrawal,
        Dividend
    }
}