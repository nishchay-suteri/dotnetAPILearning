using System.Text.Json;
using LearningAPI;
using LearningAPI.BackgroundServices;
using LearningAPI.Helpers;
using LearningAPI.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LearningAPITest.BackgroundServices;

// Making testable class to test protected method
public class RequestConsumerTestable: RequestConsumer
{
    public RequestConsumerTestable(ILogger<RequestConsumer> logger, IServiceBusHelper serviceBusHelper, IDatabaseHelper databaseHelper) : base(logger, serviceBusHelper, databaseHelper)
    {

    }

    public async Task TestableExecuteAsync(CancellationToken stoppingToken)
    {
        await ExecuteAsync(stoppingToken);
    }

}

public class RequestConsumerTests
{
    private readonly ILogger<RequestConsumer> _loggerMock;
    private readonly IServiceBusHelper _serviceBusHelperMock;
    private readonly IDatabaseHelper _databaseHelperMock;
    private readonly RequestConsumerTestable _requestConsumer;

    public RequestConsumerTests()
    {
        _loggerMock = Substitute.For<ILogger<RequestConsumer>>();
        _serviceBusHelperMock = Substitute.For<IServiceBusHelper>();
        _databaseHelperMock = Substitute.For<IDatabaseHelper>();

        _requestConsumer = new RequestConsumerTestable(_loggerMock, _serviceBusHelperMock, _databaseHelperMock);
    }

    [Fact]
    public async void ExecuteAsync_CancellationTokenRequested_StopsGracefully()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        _serviceBusHelperMock.ReceiveBulkMessagesAsync(cancellationTokenSource.Token).Returns([]);

        // Act
        var executeTask = _requestConsumer.TestableExecuteAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        await executeTask;

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("Starting background service iteration");
        _loggerMock.Received(1).LogWarning("Background service Iteration was cancelled");
        await _databaseHelperMock.DidNotReceive().CreateDataAsync(Arg.Any<DownloadDataInformation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async void ExecuteAsync_NoMessagesReceived_NoOperation()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        _serviceBusHelperMock.ReceiveBulkMessagesAsync(cancellationTokenSource.Token).Returns([]);

        // Act
        var executeTask = _requestConsumer.TestableExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(100); // Wait a bit for the method to start executing
        cancellationTokenSource.Cancel();
        await executeTask;

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("Starting background service iteration");
        _loggerMock.Received(1).LogInformation("No messages received");
        _loggerMock.Received(1).LogWarning("Background service Iteration was cancelled");
        await _databaseHelperMock.DidNotReceive().CreateDataAsync(Arg.Any<DownloadDataInformation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async void ExecuteAsync_EmptyMessageReceived_NoOperation()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        _serviceBusHelperMock.ReceiveBulkMessagesAsync(cancellationTokenSource.Token).Returns([""]);

        // Act
        var executeTask = _requestConsumer.TestableExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(100); // Wait a bit for the method to start executing
        cancellationTokenSource.Cancel();
        await executeTask;

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("Starting background service iteration");
        _loggerMock.Received(1).LogInformation("Message received is empty");
        _loggerMock.Received(1).LogWarning("Background service Iteration was cancelled");
        await _databaseHelperMock.DidNotReceive().CreateDataAsync(Arg.Any<DownloadDataInformation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async void ExecuteAsync_ValidMessageReceived_CreateEntryInDB()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var testUrl = "testUrl";
        var testDto = new DataDownloadRequestDto(testUrl);
        string serializedMessage = JsonSerializer.Serialize(testDto);
        _serviceBusHelperMock.ReceiveBulkMessagesAsync(cancellationTokenSource.Token).Returns([serializedMessage]);

        // Act
        var executeTask = _requestConsumer.TestableExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(100); // Wait a bit for the method to start executing
        cancellationTokenSource.Cancel();
        await executeTask;

        // Assert
        Assert.True(executeTask.IsCompleted);
        _loggerMock.Received(1).LogInformation("Starting background service iteration");
        _loggerMock.Received(1).LogInformation($"Received Message: {serializedMessage}");
        _loggerMock.Received(1).LogWarning("Background service Iteration was cancelled");
        await _databaseHelperMock.Received(1).CreateDataAsync(Arg.Is<DownloadDataInformation>(
            data => data.DownloadUrl == testUrl && data.TaskStatus == TaskStatusValue.New
        ), Arg.Any<CancellationToken>());
    }
}
