using Azure.Messaging.ServiceBus;
using LearningAPI.Constants;

namespace LearningAPI.Helpers;

public class ServiceBusHelper : IServiceBusHelper
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _serviceBusSender;
    private readonly ServiceBusReceiver _serviceBusReceiver;
    private readonly ILogger<ServiceBusHelper> _logger;
    private readonly IConfiguration _configuration;

    public ServiceBusHelper(ServiceBusClient serviceBusClient, ILogger<ServiceBusHelper> logger, IConfiguration configuration)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
        _configuration = configuration;
        _serviceBusSender = _serviceBusClient.CreateSender(_configuration["ServiceBus:QueueName"]);
        _serviceBusReceiver = _serviceBusClient.CreateReceiver(_configuration["ServiceBus:QueueName"]);
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            ServiceBusMessage serviceBusMessage = new(message);
            _logger.LogInformation($"Sending message: {message}");
            await _serviceBusSender.SendMessageAsync(serviceBusMessage, cancellationToken);
            _logger.LogInformation($"Message sent: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred when sending message to queue");
            throw;
        }
    }

    public async Task<string> ReceiveMessageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // the received message is a different type as it contains some service set properties
            ServiceBusReceivedMessage? receivedMessage = await _serviceBusReceiver.ReceiveMessageAsync(cancellationToken: cancellationToken);
            if (receivedMessage is null)
            {
                _logger.LogInformation($"No message found in queue");
                return string.Empty;
            }
            // get the message body as a string
            string body = receivedMessage.Body.ToString();
            _logger.LogInformation($"Received message: {body}");
            // complete the message, thereby deleting it from the service
            await _serviceBusReceiver.CompleteMessageAsync(receivedMessage, cancellationToken);
            return body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred when receiving message from queue");
            throw;
        }
    }

    public async Task<List<string>> ReceiveBulkMessagesAsync(CancellationToken cancellationToken = default)
    {
        // receive bulk messages from service bus
        var messages = new List<string>();
        var receivedMessages = await _serviceBusReceiver.ReceiveMessagesAsync(
                                                                                                    ServiceBusConstants.MaxMessagesToReceive,
                                                                                                    TimeSpan.FromSeconds(ServiceBusConstants.MaxWaitTimeForMessages),
                                                                                                    cancellationToken);
        foreach (var receivedMessage in receivedMessages)
        {
            // get the message body as a string
            string body = receivedMessage.Body.ToString();
            _logger.LogInformation($"Received message: {body}");
            messages.Add(body);
            await _serviceBusReceiver.CompleteMessageAsync(receivedMessage, cancellationToken);
        }
        return messages;
    }
}