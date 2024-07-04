using LearningAPI.Constants;
using LearningAPI.Helpers;
using LearningAPI.Models;

namespace LearningAPI.BackgroundServices;

public class DownloadDataService : BackgroundService
{
    private readonly ILogger<DownloadDataService> _logger;
    private readonly IDatabaseHelper _databaseHelper;
    private readonly IDataDownloaderHelper _dataDownloaderHelper;
    private readonly IBlobServiceHelper _blobServiceHelper;

    public DownloadDataService(
        ILogger<DownloadDataService> logger,
        IDatabaseHelper databaseHelper,
        IDataDownloaderHelper dataDownloaderHelper,
        IBlobServiceHelper blobServiceHelper)
    {
        _logger = logger;
        _databaseHelper = databaseHelper;
        _dataDownloaderHelper = dataDownloaderHelper;
        _blobServiceHelper = blobServiceHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting background service iteration");
            await ProcessDownloadDataAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Repeat every 1 minute
        }
    }

    private async Task ProcessDownloadDataAsync(CancellationToken stoppingToken)
    {
        // Imagine complex data processing here.
        _logger.LogInformation("Processing data...");
        try
        {
            var downloadDataInformation = await _databaseHelper.GetNewDataAsync(stoppingToken);

            if (downloadDataInformation.Count == 0)
            {
                _logger.LogInformation("No new data to process");
            }
            else
            {
                foreach (var data in downloadDataInformation)
                {
                    _logger.LogInformation($"Processing data for {data.Id} - {data.DownloadUrl}");
                    // Step 1) Download data to a file using data.DownloadUrl and save it to a file
                    var response = await _dataDownloaderHelper.DownloadDataAsync(data.DownloadUrl, stoppingToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var fileContent = await response.Content.ReadAsStringAsync(stoppingToken);
                        // Upload this file to Azure Blob Storage
                        bool uploadStatus = await _blobServiceHelper.UploadBlobAsync(BlobServiceConstants.ContainerName,
                                                                                    $"{data.Id}.json",
                                                                                    fileContent,
                                                                                    stoppingToken);
                        if (uploadStatus)
                        {
                            _logger.LogInformation($"Data downloaded and uploaded to Azure Blob Storage");
                            data.TaskStatus = TaskStatusValue.Completed;
                            data.UpdatedOn = DateTime.UtcNow;
                        }
                        else
                        {
                            _logger.LogError($"Error occurred while uploading data for {data.Id}");
                            data.TaskStatus = TaskStatusValue.FileUploadError;
                            data.UpdatedOn = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        _logger.LogError($"Error occurred while downloading data for {data.Id}");
                        data.TaskStatus = TaskStatusValue.FileDownloadError;
                        data.UpdatedOn = DateTime.UtcNow;
                    }
                    await _databaseHelper.UpdateDataAsync(data, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing data");
        }
    }

}
