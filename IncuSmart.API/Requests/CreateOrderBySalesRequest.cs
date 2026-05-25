using System.Text.Json.Serialization;

namespace IncuSmart.API.Requests
{
    public class CreateOrderBySalesRequest
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = CommonConst.AtLeastOneItemRequired)]
        public List<OrderItemRequest> Items { get; set; } = [];

        [JsonIgnore]
        public Guid CreatedByUserId { get; set; }
    }
}
