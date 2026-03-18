using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class Masterboard : BaseDomain<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public string? SerialNumber { get; set; }
        public string? MacAddress { get; set; }
        public string? FirmwareVersion { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }

}
