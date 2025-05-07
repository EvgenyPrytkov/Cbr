using Cbr.Dtos;
using Cbr.Services;

using Microsoft.Extensions.Caching.Memory;

using Moq.Protected;
using Moq;

using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using System.Runtime;
using Cbr.Configuration;

namespace Cbr.Tests
{
    public class CurrencyServiceTests
    {
        public CurrencyServiceTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Fact]
        public async Task GetCurrenciesAsync_ReturnsCachedResult_IfAvailable()
        {
            // Arrange
            var optionsMock = new Mock<IOptionsSnapshot<CbrOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new CbrOptions
            {
                CurrencyApiUrl = "https://test.url"
            });

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var httpClientFactory = Mock.Of<IHttpClientFactory>();

            var date = new DateTime(2024, 01, 01);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var expected = new List<CurrencyDto> { new CurrencyDto { CharCode = "USD" } };
            cache.Set($"CurrencyRate_{date:yyyy-MM-dd}", expected);

            var factoryMock = new Mock<IHttpClientFactory>();
            var service = new CurrencyService(cache, factoryMock.Object, optionsMock.Object);

            // Act
            var result = await service.GetCurrenciesAsync(date);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetCurrenciesAsync_FetchesFromCbr_IfNotCached()
        {
            // Arrange
            var optionsMock = new Mock<IOptionsSnapshot<CbrOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new CbrOptions
            {
                CurrencyApiUrl = "https://test.url"
            });

            var date = new DateTime(2024, 01, 01);
            var cache = new MemoryCache(new MemoryCacheOptions());

            var xml = @"<?xml version=""1.0"" encoding=""windows-1251""?>
                        <ValCurs Date=""01.01.2024"" name=""Foreign Currency Market"">
                            <Valute>
                                <NumCode>840</NumCode>
                                <CharCode>USD</CharCode>
                                <Nominal>1</Nominal>
                                <Name>US Dollar</Name>
                                <Value>74,32</Value>
                                <VunitRate>74,32</VunitRate>
                            </Valute>
                        </ValCurs>";

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream(Encoding.GetEncoding("windows-1251").GetBytes(xml)))
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new CurrencyService(cache, factoryMock.Object, optionsMock.Object);

            // Act
            var result = await service.GetCurrenciesAsync(date);

            // Assert
            Assert.Single(result);
            Assert.Equal("USD", result[0].CharCode);
            Assert.Equal(74.32m, result[0].Value);
        }

        [Fact]
        public async Task GetCurrenciesAsync_ThrowsException_IfCbrFails()
        {
            // Arrange
            var optionsMock = new Mock<IOptionsSnapshot<CbrOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new CbrOptions
            {
                CurrencyApiUrl = "https://test.url"
            });

            var date = new DateTime(2024, 01, 01);
            var cache = new MemoryCache(new MemoryCacheOptions());

            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new CurrencyService(cache, factoryMock.Object, optionsMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.GetCurrenciesAsync(date));
        }
    }
}