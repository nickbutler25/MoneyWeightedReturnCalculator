using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MWR.Core.Models;

namespace MWR.Core.Services
{
    public interface IDataProvider
    {
        Task<List<Transaction>> GetTransactionsAsync(string source);
        Task<PortfolioSnapshot> GetCurrentPortfolioValueAsync(string source);
    }
}