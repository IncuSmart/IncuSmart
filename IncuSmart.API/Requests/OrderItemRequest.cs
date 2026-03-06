namespace IncuSmart.API.Requests
{
    public class OrderItemRequest
    {
        [Required]
        public Guid IncubatorModelId { get; set; }

        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100s")]
        public int Quantity { get; set; }
    }
}
