using System.Net;
using LearningAPI.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LearningAPITest.Helpers;

public class TestHttpMessageHandler : DelegatingHandler
{
    private readonly HttpResponseMessage _testResponse;

    public TestHttpMessageHandler(HttpResponseMessage httpResponseMessage)
    {
        _testResponse = httpResponseMessage;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_testResponse);
    }
}

public class DataDownloaderHelperTests
{
    private readonly IHttpClientFactory _httpClientFactoryMock;
    private readonly ILogger<DataDownloaderHelper> _loggerMock;
    private readonly DataDownloaderHelper _dataDownloaderHelper;

    public DataDownloaderHelperTests()
    {
        _httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        _loggerMock = Substitute.For<ILogger<DataDownloaderHelper>>();
        _dataDownloaderHelper = new DataDownloaderHelper(_httpClientFactoryMock, _loggerMock);
    }

    [Fact]
    public async Task DownloadDataAsync_ValidUrl_DownloadDataSuccess()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var testDownloadUrl = "https://testDownloadUrl.com";
        var testStatusCode = HttpStatusCode.OK;

        var testResponseMessage = new HttpResponseMessage(testStatusCode);
        var testHttpHandler = new TestHttpMessageHandler(testResponseMessage);

        HttpClient testHttpClient = new HttpClient(testHttpHandler);
        _httpClientFactoryMock.CreateClient().Returns(testHttpClient);
        
        // Act
        var response = await _dataDownloaderHelper.DownloadDataAsync(testDownloadUrl, cancellationTokenSource.Token);

        // Assert
        Assert.Equal(testStatusCode, response.StatusCode);
        _loggerMock.Received(1).LogInformation($"Data download status for {testDownloadUrl} is : {testStatusCode}");

    }

    [Fact]
    public async Task DownloadDataAsync_TokenCancelled_ThrowsException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var testDownloadUrl = "https://testDownloadUrl.com";

        var testHttpClient = new HttpClient();
        _httpClientFactoryMock.CreateClient().Returns(testHttpClient);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => _dataDownloaderHelper.DownloadDataAsync(testDownloadUrl, cancellationTokenSource.Token));
        _loggerMock.DidNotReceive().LogInformation($"Data download status for {testDownloadUrl} is : {HttpStatusCode.OK}");

    }

}
