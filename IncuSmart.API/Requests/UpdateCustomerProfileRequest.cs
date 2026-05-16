namespace IncuSmart.API.Requests
{
    public class UpdateCustomerProfileRequest
    {
        [MaxLength(255, ErrorMessage = "Address must be at most 255 characters")]
        public string? Address { get; set; }
    }
}
