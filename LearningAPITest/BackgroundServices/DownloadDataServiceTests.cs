using System.Net;
using LearningAPI.BackgroundServices;
using LearningAPI.Helpers;
using LearningAPI.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LearningAPITest.BackgroundServices;

// Making testable class to test protected method
public class DownloadDataServiceTestable : DownloadDataService
{
    public DownloadDataServiceTestable(ILogger<DownloadDataService> logger, IDatabaseHelper databaseHelper, IDataDownloaderHelper dataDownloaderHelper, IBlobServiceHelper blobServiceHelper) : base(logger, databaseHelper, dataDownloaderHelper, blobServiceHelper)
    {
    }

    public async Task TestableExecuteAsync(CancellationToken stoppingToken)
    {
        await ExecuteAsync(stoppingToken);
    }
}

public class DownloadDataServiceTests
{
    private readonly ILogger<DownloadDataService> _loggerMock;
    private readonly IDatabaseHelper _databaseHelperMock;
    private readonly IDataDownloaderHelper _dataDownloaderHelperMock;
    private readonly IBlobServiceHelper _blobServiceHelperMock;
    private readonly DownloadDataServiceTestable _downloadDataService;

    public DownloadDataServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<DownloadDataService>>();
        _databaseHelperMock = Substitute.For<IDatabaseHelper>();
        _dataDownloaderHelperMock = Substitute.For<IDataDownloaderHelper>();
        _blobServiceHelperMock = Substitute.For<IBlobServiceHelper>();
        _downloadDataService = new DownloadDataServiceTestable(_loggerMock, _databaseHelperMock, _dataDownloaderHelperMock, _blobServiceHelperMock);
    }

    [Fact]
    public async void ExecuteAsync_CancellationTokenRequested_StopsGracefully()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var executeTask = _downloadDataService.TestableExecuteAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        await executeTask;

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("Starting background service iteration");
        await _dataDownloaderHelperMock.DidNotReceive().DownloadDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async void ExecuteAsync_NoDataToProcess_NoOperation()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        _databaseHelperMock.GetNewDataAsync(cancellationTokenSource.Token).Returns([]);

        // Act
        var executeTask = _downloadDataService.TestableExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(100); // Wait a bit for the method to start executing
        cancellationTokenSource.Cancel();
        await executeTask; // Ensure graceful shutdown

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("No new data to process");
        await _databaseHelperMock.Received(1).GetNewDataAsync(cancellationTokenSource.Token);
        await _dataDownloaderHelperMock.DidNotReceive().DownloadDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _databaseHelperMock.DidNotReceive().UpdateDataAsync(Arg.Any<DownloadDataInformation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async void ExecuteAsync_SuccessfulDownloadAndUpload_UpdatesDataAsCompleted()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var testId = 100;
        var testUrl = "testUrl";
        var testDataInfo = new DownloadDataInformation()
        {
            DownloadUrl = testUrl,
            Id = testId,
            CreatedOn = DateTime.Now,
            TaskStatus = TaskStatusValue.New
        };
        string? testFileContent = "test content";

        var downloadDataResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(testFileContent) };

        _databaseHelperMock.GetNewDataAsync(cancellationTokenSource.Token).Returns(new List<DownloadDataInformation>() { testDataInfo });
        _dataDownloaderHelperMock.DownloadDataAsync(testUrl, cancellationTokenSource.Token).Returns(downloadDataResponse);
        _blobServiceHelperMock.UploadBlobAsync(Arg.Any<string>(), $"{testId}.json", testFileContent, cancellationTokenSource.Token).Returns(true);

        // Act
        var executeTask = _downloadDataService.TestableExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(100); // Wait a bit for the method to start executing
        cancellationTokenSource.Cancel();
        await executeTask; // Ensure graceful shutdown

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("Processing data for 100 - testUrl");
        _loggerMock.Received(1).LogInformation("Data downloaded and uploaded to Azure Blob Storage");
        await _databaseHelperMock.Received(1).GetNewDataAsync(cancellationTokenSource.Token);
        await _dataDownloaderHelperMock.Received(1).DownloadDataAsync(testUrl, cancellationTokenSource.Token);
        await _blobServiceHelperMock.Received(1).UploadBlobAsync(Arg.Any<string>(), $"{testId}.json", testFileContent, cancellationTokenSource.Token);
        await _databaseHelperMock.Received(1).UpdateDataAsync(Arg.Is<DownloadDataInformation>(
            data => data.Id == testId && data.TaskStatus == TaskStatusValue.Completed),
            cancellationTokenSource.Token);
        _loggerMock.DidNotReceive().LogInformation("No new data to process");
    }

    [Fact]
    public async void ExecuteAsync_FailedDownload_UpdatesDataAsFileDownloadError()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var testId = 100;
        var testUrl = "testUrl";
        var testDataInfo = new DownloadDataInformation()
        {
            DownloadUrl = testUrl,
            Id = testId,
            CreatedOn = DateTime.Now,
            TaskStatus = TaskStatusValue.New
        };

        var downloadDataResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _databaseHelperMock.GetNewDataAsync(cancellationTokenSource.Token).Returns(new List<DownloadDataInformation>() { testDataInfo });
        _dataDownloaderHelperMock.DownloadDataAsync(testUrl, cancellationTokenSource.Token).Returns(downloadDataResponse);

        // Act
        var executeTask = _downloadDataService.TestableExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(100); // Wait a bit for the method to start executing
        cancellationTokenSource.Cancel();
        await executeTask; // Ensure graceful shutdown

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("Processing data for 100 - testUrl");
        _loggerMock.Received(1).LogError("Error occurred while downloading data for 100");
        await _databaseHelperMock.Received(1).GetNewDataAsync(cancellationTokenSource.Token);
        await _dataDownloaderHelperMock.Received(1).DownloadDataAsync(testUrl, cancellationTokenSource.Token);
        await _databaseHelperMock.Received(1).UpdateDataAsync(Arg.Is<DownloadDataInformation>(
            data => data.Id == testId && data.TaskStatus == TaskStatusValue.FileDownloadError),
            cancellationTokenSource.Token);

        await _blobServiceHelperMock.DidNotReceive().UploadBlobAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _loggerMock.DidNotReceive().LogInformation("No new data to process");
    }

    [Fact]
    public async void ExecuteAsync_FailedUpload_UpdatesDataAsFileUploadError()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var testId = 100;
        var testUrl = "testUrl";
        var testDataInfo = new DownloadDataInformation()
        {
            DownloadUrl = testUrl,
            Id = testId,
            CreatedOn = DateTime.Now,
            TaskStatus = TaskStatusValue.New
        };
        string? testFileContent = "test content";

        var downloadDataResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(testFileContent) };

        _databaseHelperMock.GetNewDataAsync(cancellationTokenSource.Token).Returns(new List<DownloadDataInformation>() { testDataInfo });
        _dataDownloaderHelperMock.DownloadDataAsync(testUrl, cancellationTokenSource.Token).Returns(downloadDataResponse);
        _blobServiceHelperMock.UploadBlobAsync(Arg.Any<string>(), $"{testId}.json", testFileContent, cancellationTokenSource.Token).Returns(false);

        // Act
        var executeTask = _downloadDataService.TestableExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(100); // Wait a bit for the method to start executing
        cancellationTokenSource.Cancel();
        await executeTask; // Ensure graceful shutdown

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("Processing data for 100 - testUrl");
        _loggerMock.Received(1).LogError("Error occurred while uploading data for 100");
        await _databaseHelperMock.Received(1).GetNewDataAsync(cancellationTokenSource.Token);
        await _dataDownloaderHelperMock.Received(1).DownloadDataAsync(testUrl, cancellationTokenSource.Token);
        await _blobServiceHelperMock.Received(1).UploadBlobAsync(Arg.Any<string>(), $"{testId}.json", testFileContent, cancellationTokenSource.Token);
        await _databaseHelperMock.Received(1).UpdateDataAsync(Arg.Is<DownloadDataInformation>(
            data => data.Id == testId && data.TaskStatus == TaskStatusValue.FileUploadError),
            cancellationTokenSource.Token);
        _loggerMock.DidNotReceive().LogInformation("No new data to process");
    }
}
