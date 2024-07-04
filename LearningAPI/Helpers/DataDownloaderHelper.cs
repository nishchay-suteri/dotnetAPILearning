namespace LearningAPI.Helpers;

public class DataDownloaderHelper : IDataDownloaderHelper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DataDownloaderHelper> _logger;
    public DataDownloaderHelper(IHttpClientFactory httpClientFactory, ILogger<DataDownloaderHelper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> DownloadDataAsync(string? downloadUrl, CancellationToken stoppingToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(downloadUrl, stoppingToken);
        _logger.LogInformation($"Data download status for {downloadUrl} is : {response.StatusCode}");
        return response;
    }
}
