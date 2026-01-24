using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderDetailsReserverFunction.Integration
{
    public class OrderIntegrationDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("orderID")]
        public string OrderID { get; set; }
        public DateTime CreatedAt { get; set; }
        public ShippingAddressDto ShippingAddress { get; set; }
        public List<OrderItemDto> Items { get; set; }
        public decimal FinalPrice { get; set; }
        public string Status { get; set; }
    }
}
