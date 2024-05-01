using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace TripPin.Tests;

[TestClass]
public class PeopleServiceTests
{
    private Mock<IHttpClientFactory> httpClientFactoryMock;
    private Mock<HttpMessageHandler> httpHandlerMock;
    private IConfiguration configuration;
    private Mock<ILogger<PeopleService>> loggerMock;

    [TestInitialize]
    public void Setup()
    {
        httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(httpHandlerMock.Object);
        httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(string.Empty)).Returns(httpClient).Verifiable();

        loggerMock = new Mock<ILogger<PeopleService>>();

        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ApiBaseUrl", "https://fakeUrl/" }
            }!)
            .Build();
    }

    [TestMethod]
    public async Task Search_NoFilter_Success()
    {
        // Arrange
        var expectedUri = new Uri("https://fakeUrl/People");
        httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{ value: [{""UserName"":""TestUser""}]}"),
            })
            .Verifiable();

        var peopleService = new PeopleService(httpClientFactoryMock.Object, configuration, loggerMock.Object);

        // Act
        var result = await peopleService.Search(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].UserName.Should().Be("TestUser");
        httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get
                && req.RequestUri == expectedUri
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [TestMethod]
    public async Task Search_WithFilter_Success()
    {
        // Arrange
        var expectedUri = new Uri("https://fakeUrl/People?$filter=FirstName eq 'Test'");
        httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            // prepare the expected response of the mocked http call
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{ value: [{""UserName"":""TestUser""}]}"),
            })
            .Verifiable();

        var peopleService = new PeopleService(httpClientFactoryMock.Object, configuration, loggerMock.Object);

        // Act
        var result = await peopleService.Search("FirstName eq 'Test'");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].UserName.Should().Be("TestUser");
        httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get
                    && req.RequestUri == expectedUri
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [TestMethod]
    public async Task Search_ApiError_ThrowException()
    {
        // Arrange
        httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(@"some error"),
            })
            .Verifiable();

        var peopleService = new PeopleService(httpClientFactoryMock.Object, configuration, loggerMock.Object);

        // Act
        var act = async () =>
        {
            _ = await peopleService.Search(null);
        };
        
        // Assert
        await act.Should().ThrowAsync<WebException>();
        loggerMock.Verify(x=>x.Log(LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
    
    [TestMethod]
    public async Task Search_InvalidResponseSchema_ThrowException()
    {
        // Arrange
        var expectedUri = new Uri("https://fakeUrl/People");
        httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{ unexpectedKey: [{""UserName"":""TestUser""}]}"),
            })
            .Verifiable();

        var peopleService = new PeopleService(httpClientFactoryMock.Object, configuration, loggerMock.Object);

        // Act
        var act = async () =>
        {
            _  = await peopleService.Search(null);
        };
        
        // Assert
        await act.Should().ThrowAsync<WebException>();
        loggerMock.Verify(x=>x.Log(LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
    
    // TODO test rest of the methods
}