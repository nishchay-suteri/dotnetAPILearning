using LearningAPI.Data;
using LearningAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningAPI.Helpers;

public class DatabaseHelper : IDatabaseHelper
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseHelper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task CreateDataAsync(DownloadDataInformation data, CancellationToken stoppingToken)
    {
        var dbContext = GetDbContext();
        dbContext.Add(data);
        await dbContext.SaveChangesAsync(stoppingToken);
    }

    public async Task<List<DownloadDataInformation>> GetNewDataAsync(CancellationToken stoppingToken)
    {
        var dbContext = GetDbContext();
        var downloadDataInformation = await dbContext.DataInformation
                                                                    .Where(x => x.TaskStatus == TaskStatusValue.New)
                                                                    .ToListAsync(stoppingToken);
        return downloadDataInformation;
    }

    public async Task UpdateDataAsync(DownloadDataInformation data, CancellationToken stoppingToken)
    {
        var dbContext = GetDbContext();
        dbContext.Update(data);
        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private DownloadDataContext GetDbContext()
    {
        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DownloadDataContext>();
    }
}
