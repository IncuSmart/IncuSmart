using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class CreateIncubatorRequest
    {
        [Required(ErrorMessage = "QrCode is required")]
        [MinLength(2, ErrorMessage = "QrCode must be at least 2 characters")]
        [MaxLength(255, ErrorMessage = "QrCode must be at most 255 characters")]
        public string QrCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "ModelId is required")]
        public Guid ModelId { get; set; }

        public Guid? CustomerId { get; set; }
        public DateTime? ActivatedAt { get; set; }
    }
}
