using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Events;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.ApplicationCore.Messaging;
public class ServiceBusOrderEventPublisher : IOrderEventPublisher
{
    private readonly ServiceBusClient _client;
    private const string TopicName = "order-created";

    public ServiceBusOrderEventPublisher(IConfiguration configuration)
    {
        _client = new ServiceBusClient(
            configuration.GetConnectionString("ServiceBusConnection"));
    }

    public async Task PublishOrderCreatedAsync(Order order)
    {
        var sender = _client.CreateSender(TopicName);

        var evt = new OrderCreatedEvent
        {
            OrderId = order.Id,
            Items = order.OrderItems.Select(i => new OrderItemDto
            {
                ItemId = i.ItemOrdered.CatalogItemId,
                Quantity = i.Units
            }).ToList()
        };

        var json = JsonSerializer.Serialize(evt);

        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message);
    }
}
