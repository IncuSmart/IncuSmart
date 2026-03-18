using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("maintenance_tickets")]
    public class MaintenanceTicketEntity : BaseEntity<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public Guid TechnicianId { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
