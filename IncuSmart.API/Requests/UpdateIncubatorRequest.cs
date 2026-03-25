using IncuSmart.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class UpdateIncubatorRequest
    {
        [MinLength(2, ErrorMessage = "QrCode must be at least 2 characters")]
        [MaxLength(255, ErrorMessage = "QrCode must be at most 255 characters")]
        public string? QrCode { get; set; }

        public Guid? ModelId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public IncubatorStatus? Status { get; set; }
    }
}
