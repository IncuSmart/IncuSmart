namespace IncuSmart.API.Requests
{
    public class UpdateIncubatorStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
