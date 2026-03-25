using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class CreateIncubatorCommand
    {
        public string QrCode { get; set; } = string.Empty;
        public Guid ModelId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? ActivatedAt { get; set; }
    }
}
