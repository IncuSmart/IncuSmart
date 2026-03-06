using System.Text.Json.Serialization;

namespace IncuSmart.API.Requests
{
    public class CreateOrderByCustomerRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<OrderItemRequest> Items { get; set; } = [];

        [JsonIgnore]
        public Guid UserId { get; set; }
    }
}
