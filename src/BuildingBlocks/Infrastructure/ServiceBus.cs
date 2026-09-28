using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;

/// <summary>
/// Configuration used by shared Azure Service Bus components.
/// </summary>
public sealed class ServiceBusOptions
{
    /// <summary>
    /// Gets or sets the fully qualified Service Bus namespace.
    /// </summary>
    public string FullyQualifiedNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of concurrent message handlers.
    /// Consumers may use this value when configuring their processor.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 8;
}

/// <summary>
/// Publishes application events to Azure Service Bus topics.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a JSON-serialized message to the specified topic.
    /// </summary>
    /// <typeparam name="T">The contract type being published.</typeparam>
    /// <param name="topic">The target Service Bus topic.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="correlationId">The distributed correlation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishAsync<T>(
        string topic,
        T message,
        string correlationId,
        CancellationToken ct = default);
}

/// <summary>
/// Azure Service Bus publisher implementation using either a connection string or managed identity.
/// </summary>
public sealed class ServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private const string SchemaVersionPropertyName = "schemaVersion";
    private const string SchemaVersion = "1";

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ServiceBusClient _client;

    /// <summary>
    /// Initializes the publisher from shared Service Bus settings.
    /// </summary>
    /// <param name="options">Configured Service Bus options.</param>
    public ServiceBusPublisher(IOptions<ServiceBusOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var settings = options.Value;

        if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            _client = new ServiceBusClient(settings.ConnectionString);
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.FullyQualifiedNamespace))
        {
            throw new InvalidOperationException(
                "ServiceBus:FullyQualifiedNamespace must be configured when a connection string is not provided.");
        }

        _client = new ServiceBusClient(
            settings.FullyQualifiedNamespace,
            new DefaultAzureCredential());
    }

    /// <summary>
    /// Publishes a message with standard content-type, correlation, message-id and schema metadata.
    /// </summary>
    public async Task PublishAsync<T>(
        string topic,
        T message,
        string correlationId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic is required.", nameof(topic));
        }

        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException(
                "Correlation ID is required.",
                nameof(correlationId));
        }

        await using ServiceBusSender sender = _client.CreateSender(topic);

        var payload = JsonSerializer.Serialize(message, SerializerOptions);

        var serviceBusMessage = new ServiceBusMessage(payload)
        {
            ContentType = "application/json",
            CorrelationId = correlationId.Trim(),
            MessageId = Guid.NewGuid().ToString("N")
        };

        serviceBusMessage.ApplicationProperties[SchemaVersionPropertyName] =
            SchemaVersion;

        await sender.SendMessageAsync(serviceBusMessage, ct);
    }

    /// <summary>
    /// Releases the underlying Service Bus client.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }
}