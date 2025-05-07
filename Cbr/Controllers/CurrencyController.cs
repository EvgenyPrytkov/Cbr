using Cbr.Dtos;
using Cbr.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Cbr.Controllers
{
    [ApiController]
    [Route("api/currencyRate")]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        /// <summary>
        /// Gets exchange rates from the Central Bank of Russia
        /// </summary>
        /// <param name="date">Date in format yyyy-MM-dd</param>
        /// <param name="code">Currency code, e.g., USD</param>
        /// <returns>List of currencies or a specific currency rate</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<CurrencyDto>), 200)]
        [ProducesResponseType(typeof(CurrencyDto), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [SwaggerOperation(
            Summary = "Gets exchange rates from the Central Bank of Russia",
            Description = "Returns a list of currencies or a specific currency rate by date. Date format: yyyy-MM-dd",
            OperationId = "GetCurrencyRate",
            Tags = new[] { "Currency" })]
        public async Task<IActionResult> GetCurrencyRate(
            [FromQuery, SwaggerParameter("Date in format yyyy-MM-dd")] string date = null,
            [FromQuery, SwaggerParameter("Currency code, e.g., USD")] string code = null)
        {
            DateTime parsedDate;
            if (!string.IsNullOrEmpty(date))
            {
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    return BadRequest("Invalid date format. Use yyyy-MM-dd");
                }
            }
            else
            {
                parsedDate = DateTime.Today;
            }

            try
            {
                var currencies = await _currencyService.GetCurrenciesAsync(parsedDate);

                if (!string.IsNullOrEmpty(code))
                {
                    var currency = currencies.FirstOrDefault(c => c.CharCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                    if (currency == null)
                    {
                        return NoContent();
                    }

                    return Ok(currency);
                }

                return Ok(currencies);
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to retrieve data from the Central Bank of Russia.");
            }
        }
    }
}
