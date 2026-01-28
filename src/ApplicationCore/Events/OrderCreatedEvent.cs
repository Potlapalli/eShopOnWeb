using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Events;
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}
