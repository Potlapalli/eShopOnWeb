using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OrderDetailsReserverFunction.Integration;

namespace OrderDetailsReserverFunction;

public class OrderDetailsReserver
{
    private readonly ILogger<OrderDetailsReserver> _logger;
    private readonly IConfiguration _configuration;

    public OrderDetailsReserver(ILogger<OrderDetailsReserver> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function("OrderDetailsReserver")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("OrderDetailsReserver function triggered.");

        // Parse the incoming request body
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var order = JsonConvert.DeserializeObject<OrderIntegrationDto>(requestBody);

        if (order == null)
        {
            return new BadRequestObjectResult("Invalid order details.");
        }

        order.Id = order.OrderID;
        order.CreatedAt = DateTime.UtcNow;
        order.Status = "Created";

        var connectionString = _configuration["CosmosDBConnection"];

        var client = new CosmosClient(connectionString);
        var container = client.GetContainer("OrderIntegrationDb", "OrderDetails");

        try
        {
            await container.CreateItemAsync(order, new PartitionKey(order.OrderID));
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error uploading order request: {ex.Message} {ex.StatusCode}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        _logger.LogInformation($"Order request uploaded successfully");

        return new OkObjectResult("Order saved to Cosmos DB");
    }
}