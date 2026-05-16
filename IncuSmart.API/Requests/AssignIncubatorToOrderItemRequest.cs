namespace IncuSmart.API.Requests
{
    public class AssignIncubatorToOrderItemRequest
    {
        [Required]
        public Guid OrderItemId { get; set; }

        [Required]
        public Guid IncubatorId { get; set; }
    }
}
