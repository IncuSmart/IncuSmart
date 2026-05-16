using System;
using System.ComponentModel.DataAnnotations;

namespace IncuSmart.API.Requests
{
    public class CreateControlDeviceRequest
    {
        [Required(ErrorMessage = "MasterboardId là bắt buộc")]
        public Guid MasterboardId { get; set; }

        // Nullable - FK tới control_board_types, Y nếu có
        public Guid? ControlBoardTypesId { get; set; }

        [Required(ErrorMessage = "ConfigId là bắt buộc")]
        public Guid ConfigId { get; set; }

        // Mã phần cứng
        [MaxLength(50, ErrorMessage = "HardwareCode không được vượt quá 50 ký tự")]
        public string? HardwareCode { get; set; }

        // Chân pin
        public int? PinNumber { get; set; }

        // ON | OFF
        [MaxLength(20, ErrorMessage = "State không được vượt quá 20 ký tự")]
        public string? State { get; set; }
    }
}
