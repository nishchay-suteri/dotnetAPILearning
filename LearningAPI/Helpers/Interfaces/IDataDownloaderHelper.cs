namespace LearningAPI.Helpers;

public interface IDataDownloaderHelper
{
    Task<HttpResponseMessage> DownloadDataAsync(string? downloadUrl, CancellationToken stoppingToken);
}
