namespace LearningAPI.Helpers;

public interface IServiceBusHelper
{
    public Task SendMessageAsync(string message, CancellationToken cancellationToken = default(CancellationToken));
    public Task<List<string>> ReceiveBulkMessagesAsync(CancellationToken cancellationToken = default(CancellationToken));
}