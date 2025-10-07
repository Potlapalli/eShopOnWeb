using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;



namespace OrderItemsReserverFunction
{
    public class OrderItemsReserver
    {
        private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=eshoponwebstrg;AccountKey=7qjJTsSQrgi8VZQEWDOYrcyO8u7gQELtVu0EtjsLQADVEGRyehEKIEACD9VsQ6gPjiDiQpptEDEE+AStt8BARQ==;EndpointSuffix=core.windows.net";
        private const string ContainerName = "orders";

         private readonly ILogger<OrderItemsReserver> _logger;

        public OrderItemsReserver(ILogger<OrderItemsReserver> logger)
        {
            _logger = logger;
        }

        [Function("OrderItemsReserver")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            //_logger.LogInformation("C# HTTP trigger function processed a request.");
            //return new OkObjectResult("Welcome to Azure Functions!");

            _logger.LogInformation("OrderItemsReserver function triggered.");

            // Parse the incoming request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var orderDetails = JsonConvert.DeserializeObject(requestBody);

            if (orderDetails == null)
            {
                return new BadRequestObjectResult("Invalid order details.");
            }

            // Generate a unique file name
            string fileName = $"order-{Guid.NewGuid()}.json";

            // Initialize Blob Storage client
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            // Upload the JSON file to Blob Storage
            var blobClient = containerClient.GetBlobClient(fileName);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody)))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            _logger.LogInformation($"Order request uploaded successfully: {fileName}");
            return new OkObjectResult($"Order request uploaded: {fileName}");

        }
    }
}
