using System.Text.Json;
using LearningAPI.Helpers;
using LearningAPI.Models;

namespace LearningAPI.BackgroundServices;
public class RequestConsumer : BackgroundService
{
    private readonly ILogger<RequestConsumer> _logger;
    private readonly IServiceBusHelper _serviceBusHelper;
    private readonly IDatabaseHelper _databaseHelper;
    public RequestConsumer(ILogger<RequestConsumer> logger, IServiceBusHelper serviceBusHelper, IDatabaseHelper databaseHelper)
    {
        _logger = logger;
        _serviceBusHelper = serviceBusHelper;
        _databaseHelper = databaseHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting background service iteration");
            try
            {
                // await ConsumeRequestAsync(stoppingToken);
                await ConsumeMultipleRequestsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Repeat every 1 minute
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Background service Iteration was cancelled");
            }
        }
    }

    private async Task ConsumeMultipleRequestsAsync(CancellationToken stoppingToken)
    {
        // Imagine complex data processing here.
        _logger.LogInformation("Processing Multiple data...");
        List<string> messagesReceived = await _serviceBusHelper.ReceiveBulkMessagesAsync(stoppingToken);
        if (messagesReceived.Count == 0)
        {
            _logger.LogInformation("No messages received");
        }
        else
        {
            foreach (var message in messagesReceived)
            {
                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogInformation("Message received is empty");
                }
                else
                {
                    _logger.LogInformation($"Received Message: {message}");
                    var receivedMessageObject = JsonSerializer.Deserialize<DataDownloadRequestDto>(message);
                    var downloadDataInformation = new DownloadDataInformation()
                    {
                        DownloadUrl = receivedMessageObject?.DownloadUrl,
                        TaskStatus = TaskStatusValue.New,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    };
                    await _databaseHelper.CreateDataAsync(downloadDataInformation, stoppingToken);
                }
            }
        }
    }
}