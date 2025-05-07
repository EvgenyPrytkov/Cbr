using Cbr.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cbr.Services
{
    public interface ICurrencyService
    {
        Task<List<CurrencyDto>> GetCurrenciesAsync(DateTime date);
    }
}
