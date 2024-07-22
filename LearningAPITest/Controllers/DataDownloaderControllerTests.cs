using LearningAPI;
using LearningAPI.Controllers;
using LearningAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;


namespace LearningAPITest.Controllers;

public class DataDownloaderControllerTests
{
    private readonly ILogger<DataDownloaderController> _loggerMock;
    private readonly IServiceBusHelper _serviceBusHelperMock;
    private readonly DataDownloaderController _dataDownloaderController;

    public DataDownloaderControllerTests()
    {
        _loggerMock = Substitute.For<ILogger<DataDownloaderController>>();
        _serviceBusHelperMock = Substitute.For<IServiceBusHelper>();
        _dataDownloaderController = new DataDownloaderController(_loggerMock, _serviceBusHelperMock);
    }

    [Fact]
    public async void DownloadData_EmptyUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new DataDownloadRequestDto(string.Empty);

        // Act
        var result = await _dataDownloaderController.DownloadData(request, default);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Download url can't be empty", (result as BadRequestObjectResult)?.Value);
        _loggerMock.Received(1).LogWarning("Received empty url");
    }

    [Fact]
    public async void DownloadData_ValidUrl_SubmitsRequest()
    {
        // Arrange
        var request = new DataDownloadRequestDto("https://example.com");

        // Act
        var result = await _dataDownloaderController.DownloadData(request, default);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Request submitted for url: https://example.com", (result as OkObjectResult)?.Value);
        _loggerMock.Received(1).LogInformation("Received URL: https://example.com");
        await _serviceBusHelperMock.Received(1).SendMessageAsync(Arg.Is<string>(request =>
            request.Equals("{\"DownloadUrl\":\"https://example.com\"}")
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async void DownloadData_ExceptionOccurs_ReturnsInternalServerError()
    {
        // Arrange
        var request = new DataDownloadRequestDto("https://example.com");
        _serviceBusHelperMock.SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Test exception"));

        // Act
        var result = await _dataDownloaderController.DownloadData(request, default);

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, (result as StatusCodeResult)?.StatusCode);
        _loggerMock.Received(1).LogInformation("Received URL: https://example.com");
    }
}
