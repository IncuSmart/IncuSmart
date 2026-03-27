using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class CreateConfigRequest
    {
        [Required(ErrorMessage = "Code là bắt buộc")]
        [MinLength(2,  ErrorMessage = "Code phải có ít nhất 2 ký tự")]
        [MaxLength(50, ErrorMessage = "Code không được vượt quá 50 ký tự")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name là bắt buộc")]
        [MinLength(2,   ErrorMessage = "Name phải có ít nhất 2 ký tự")]
        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        // SENSOR | ACTUATOR
        [MaxLength(30, ErrorMessage = "Type không được vượt quá 30 ký tự")]
        public string? Type { get; set; }

        // Đơn vị đo: °C, %, ppm...
        [MaxLength(20, ErrorMessage = "Unit không được vượt quá 20 ký tự")]
        public string? Unit { get; set; }

        public string? Description { get; set; }
    }
}
