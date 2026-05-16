namespace IncuSmart.API.Requests
{
    public class ResolveAlertRequest
    {
        [MaxLength(1000, ErrorMessage = "Message không được vượt quá 1000 ký tự")]
        public string? Message { get; set; }
    }
}
