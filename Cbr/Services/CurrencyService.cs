using Cbr.Configuration;
using Cbr.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Cbr.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CbrOptions _cbrOptions;

        public CurrencyService(IMemoryCache cache, IHttpClientFactory httpClientFactory, IOptionsSnapshot<CbrOptions> cbrOptions)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _cbrOptions = cbrOptions.Value;
        }

        public async Task<List<CurrencyDto>> GetCurrenciesAsync(DateTime date)
        {
            string cacheKey = $"CurrencyRate_{date:yyyy-MM-dd}";

            if (_cache.TryGetValue(cacheKey, out List<CurrencyDto> cached))
            {
                return cached;
            }

            var url = $"{_cbrOptions.CurrencyApiUrl}{date:dd/MM/yyyy}";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Unable to fetch currency data from the Central Bank of Russia");
            }

            using var stream = await response.Content.ReadAsStreamAsync();

            var currencies = ParseCurrencies(stream);

            _cache.Set(cacheKey, currencies, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = date.Date.AddDays(1)
            });

            return currencies;
        }

        private List<CurrencyDto> ParseCurrencies(Stream xmlStream)
        {
            var currencies = new List<CurrencyDto>();

            using var reader = new StreamReader(xmlStream, Encoding.GetEncoding("windows-1251"));
            var xml = new XmlDocument();
            xml.Load(reader);

            foreach (XmlNode node in xml.SelectNodes("//Valute"))
            {
                currencies.Add(new CurrencyDto
                {
                    CharCode = node["CharCode"]?.InnerText,
                    Name = node["Name"]?.InnerText,
                    Nominal = node["Nominal"]?.InnerText,
                    NumCode = int.Parse(node["NumCode"]?.InnerText),
                    Value = decimal.Parse(node["Value"]?.InnerText, new CultureInfo("ru-RU")),
                    VunitRate = decimal.Parse(node["VunitRate"]?.InnerText, new CultureInfo("ru-RU")),
                });
            }

            return currencies;
        }
    }
}
