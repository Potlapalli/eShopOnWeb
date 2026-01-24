using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Configuration;


namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly string? _azureBlobFunctionUrl;
    private readonly string? _azureCosmosDBFunctionUrl;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _azureBlobFunctionUrl = _configuration["AzureFunctions:BlobFunctionUrl"];
        _azureCosmosDBFunctionUrl = _configuration["AzureFunctions:CosmosDBFunctionUrl"];

    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        var httpClient = _httpClientFactory.CreateClient();

        var orderDetails = new
        {
            orderID = order.Id.ToString(),
            shippingAddress = new
            {
                street = order.ShipToAddress.Street,
                city = order.ShipToAddress.City,
                state = order.ShipToAddress.State,
                country = order.ShipToAddress.Country,
                zipCode = order.ShipToAddress.ZipCode
            },
            items = order.OrderItems.Select(oi => new
            {
                itemId = oi.ItemOrdered.CatalogItemId,
                productName = oi.ItemOrdered.ProductName,
                quantity = oi.Units,
                unitPrice = oi.UnitPrice
            }),
            finalPrice = order.Total()
        };


        //var orderDetails = order.OrderItems.Select(oi => new
        //{
        //    ItemId = oi.ItemOrdered.CatalogItemId,
        //    Quantity = oi.Units,
        //});

        string payload = JsonSerializer.Serialize(orderDetails);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // var response = await httpClient.PostAsync(_azureFunctionUrl, content);
        var response = await httpClient.PostAsync(_azureCosmosDBFunctionUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to send order details to warehouse.");
        }
    }


}
