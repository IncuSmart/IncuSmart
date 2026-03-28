namespace IncuSmart.API.Requests
{
    public class CreateSensorRequest
    {
        [Required(ErrorMessage = "MasterboardId là bắt buộc")]
        public Guid MasterboardId { get; set; }

        [Required(ErrorMessage = "ConfigInstanceId là bắt buộc")]
        public Guid ConfigInstanceId { get; set; }

        // Mã phần cứng
        [MaxLength(50, ErrorMessage = "HardwareCode không được vượt quá 50 ký tự")]
        public string? HardwareCode { get; set; }

        // Chân pin kết nối
        public int? PinNumber { get; set; }
    }
}
