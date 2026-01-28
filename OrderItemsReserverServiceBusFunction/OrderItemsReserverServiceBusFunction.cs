using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;

namespace OrderItemsReserverServiceBusFunction;

public class OrderItemsReserverServiceBusFunction
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<OrderItemsReserverServiceBusFunction> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public OrderItemsReserverServiceBusFunction(ILogger<OrderItemsReserverServiceBusFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
        _container = new BlobContainerClient(
            _configuration["AzureBlobStorage"],
            "orders");
    }

    [Function("SBOrderItemsReserver")]
    public async Task Run(
        [ServiceBusTrigger("order-created", "warehouse-reserver", Connection = "ServiceBusConnection")] string message)

    {
        _logger.LogInformation("OrderItemsReserver function triggered.");

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2));

        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                var jsonBytes = Encoding.UTF8.GetBytes(message);
                var blobName = $"order-{Guid.NewGuid()}.json";

                using var stream = new MemoryStream(jsonBytes);
                await _container.CreateIfNotExistsAsync();
                await _container.UploadBlobAsync(blobName, stream);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob upload failed");

            // 🔁 FALLBACK → Logic App
            await _httpClient.PostAsync(
                 _configuration["LogicAppEndpoint"],
                new StringContent(message, Encoding.UTF8, "application/json"));
        }
    }
}
