using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class GuestOrderInfo : BaseDomain<BaseStatus>
    {
        public Guid OrderId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Description { get; set; }

        public string VerificationPassHash { get; set; } = string.Empty;

        public DateTime? ClaimedAt { get; set; }
        public Guid? ClaimedBy { get; set; }
    }
}
