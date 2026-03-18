using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class MaintenanceLog : BaseDomain<BaseStatus>
    {
        public Guid TicketId { get; set; }
        public string? Description { get; set; }
    }

}
