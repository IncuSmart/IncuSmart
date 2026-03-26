using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class CreateMaintenanceTicketCommand
    {
        public Guid IncubatorId { get; set; }
        public Guid TechnicianId { get; set; }
    }
    public class MaintenanceTicketDetail
    {
        public MaintenanceTicket? Ticket { get; set; }
        public List<MaintenanceLog> Logs { get; set; } = new();
    }
}
