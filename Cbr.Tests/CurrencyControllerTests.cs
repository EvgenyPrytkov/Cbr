using Cbr.Controllers;
using Cbr.Dtos;
using Cbr.Services;

using Microsoft.AspNetCore.Mvc;

using Moq;

namespace Cbr.Tests
{
    public class CurrencyControllerTests
    {
        [Fact]
        public async Task GetCurrencyRate_ReturnsAllCurrencies_CodeNotProvided()
        {
            // Arrange
            var mockService = new Mock<ICurrencyService>();
            var currencies = new List<CurrencyDto> { new() { CharCode = "USD" } };
            mockService.Setup(s => s.GetCurrenciesAsync(It.IsAny<DateTime>())).ReturnsAsync(currencies);

            var controller = new CurrencyController(mockService.Object);

            // Act
            var result = await controller.GetCurrencyRate();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(currencies, ok.Value);
        }

        [Fact]
        public async Task GetCurrencyRate_ReturnsSingleCurrency_CodeProvided()
        {
            // Arrange
            var mockService = new Mock<ICurrencyService>();
            var usd = new CurrencyDto { CharCode = "USD" };
            var list = new List<CurrencyDto> { usd };
            mockService.Setup(s => s.GetCurrenciesAsync(It.IsAny<DateTime>())).ReturnsAsync(list);

            var controller = new CurrencyController(mockService.Object);

            // Act
            var result = await controller.GetCurrencyRate(code: "usd");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(usd, ok.Value);
        }

        [Fact]
        public async Task GetCurrencyRate_ReturnsNoContent_CodeNotFound()
        {
            // Arrange
            var mockService = new Mock<ICurrencyService>();
            var list = new List<CurrencyDto> { new CurrencyDto { CharCode = "EUR" } };
            mockService.Setup(s => s.GetCurrenciesAsync(It.IsAny<DateTime>())).ReturnsAsync(list);

            var controller = new CurrencyController(mockService.Object);

            // Act
            var result = await controller.GetCurrencyRate(code: "USD");

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetCurrencyRate_ReturnsBadRequest_InvalidDateFormat()
        {
            // Arrange
            var mockService = new Mock<ICurrencyService>();
            var controller = new CurrencyController(mockService.Object);

            // Act
            var result = await controller.GetCurrencyRate(date: "not-a-date");

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid date format. Use yyyy-MM-dd", bad.Value);
        }

        [Fact]
        public async Task GetCurrencyRate_ReturnsInternalServerError_Exception()
        {
            // Arrange
            var mockService = new Mock<ICurrencyService>();
            mockService.Setup(s => s.GetCurrenciesAsync(It.IsAny<DateTime>())).ThrowsAsync(new Exception());

            var controller = new CurrencyController(mockService.Object);

            // Act
            var result = await controller.GetCurrencyRate();

            // Assert
            var error = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, error.StatusCode);
        }
    }
}