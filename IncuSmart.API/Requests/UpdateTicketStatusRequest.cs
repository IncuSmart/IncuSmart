using IncuSmart.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class UpdateTicketStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        public TicketStatus Status { get; set; } 
    }
}
