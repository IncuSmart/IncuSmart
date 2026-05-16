using System.Text.Json.Serialization;

namespace IncuSmart.API.Requests
{
    public class CreateOrderByCustomerRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = CommonConst.AtLeastOneItemRequired)]
        public List<OrderItemRequest> Items { get; set; } = [];

        [JsonIgnore]
        public Guid UserId { get; set; }
    }
}
