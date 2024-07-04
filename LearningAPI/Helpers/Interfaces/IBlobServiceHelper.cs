namespace LearningAPI.Helpers;

public interface IBlobServiceHelper
{
    public Task<bool> UploadBlobAsync(string containerName, string blobName, string fileContent, CancellationToken stoppingToken);
}
