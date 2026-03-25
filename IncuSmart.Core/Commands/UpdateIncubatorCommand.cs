using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class UpdateIncubatorCommand
    {
        public Guid Id { get; set; }
        public string? QrCode { get; set; }
        public Guid? ModelId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public IncubatorStatus? Status { get; set; }
    }
}
