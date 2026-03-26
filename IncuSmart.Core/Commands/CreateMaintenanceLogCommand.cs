using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class CreateMaintenanceLogCommand
    {
        public Guid TicketId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
