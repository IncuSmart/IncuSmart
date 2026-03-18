using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class MaintenanceTicket : BaseDomain<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public Guid TechnicianId { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

}
