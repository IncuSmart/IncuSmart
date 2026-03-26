using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class CreateMaintenanceTicketRequest
    {
        [Required(ErrorMessage = "IncubatorId is required")]
        public Guid IncubatorId { get; set; }

        [Required(ErrorMessage = "TechnicianId is required")]
        public Guid TechnicianId { get; set; }
    }
}
