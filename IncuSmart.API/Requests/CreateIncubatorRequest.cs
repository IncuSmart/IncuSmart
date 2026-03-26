using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class CreateIncubatorRequest
    {

        [Required(ErrorMessage = "ModelId is required")]
        public Guid ModelId { get; set; }

        public Guid? CustomerId { get; set; }
        public DateTime? ActivatedAt { get; set; }
    }
}
