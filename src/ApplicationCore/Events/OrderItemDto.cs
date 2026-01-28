using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Events;
public class OrderItemDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}
