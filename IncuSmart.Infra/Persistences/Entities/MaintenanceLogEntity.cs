using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("maintenance_logs")]
    public class MaintenanceLogEntity : BaseEntity<BaseStatus>
    {
        public Guid TicketId { get; set; }
        public string? Description { get; set; }
    }
}
