using LearningAPI.Models;

namespace LearningAPI.Helpers;

public interface IDatabaseHelper
{
    public Task CreateDataAsync(DownloadDataInformation data, CancellationToken stoppingToken);
    public Task UpdateDataAsync(DownloadDataInformation data, CancellationToken stoppingToken);
    public Task<List<DownloadDataInformation>> GetNewDataAsync(CancellationToken stoppingToken);
}
