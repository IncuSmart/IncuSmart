namespace IncuSmart.API.Requests
{
    public class UpdateMaintenanceTicketStatusRequest
    {
        [Required(ErrorMessage = "Status là bắt buộc")]
        [MaxLength(30, ErrorMessage = "Status không được vượt quá 30 ký tự")]
        public string Status { get; set; } = string.Empty;
    }
}
