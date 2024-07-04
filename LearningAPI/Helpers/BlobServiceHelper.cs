using System.Text;
using Azure.Storage.Blobs;

namespace LearningAPI.Helpers;

public class BlobServiceHelper : IBlobServiceHelper
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobServiceHelper> _logger;

    public BlobServiceHelper(BlobServiceClient blobServiceClient, ILogger<BlobServiceHelper> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<bool> UploadBlobAsync(string containerName, string blobName, string fileContent, CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Uploading blob {blobName} to container {containerName}");
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var content = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            var blobResponse = await blobClient.UploadAsync(content, true, stoppingToken);
            _logger.LogInformation($"Blob {blobName} uploaded to container {containerName} with ETag: {blobResponse.Value.ETag}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading blob {blobName} to container {containerName} - {ex.Message}");
            return false;
        }
    }
}