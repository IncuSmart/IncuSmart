using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class ModelConfigItem
    {
        [Required(ErrorMessage = "ConfigId is required")]
        public Guid ConfigId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int? Quantity { get; set; }

        public bool? Required { get; set; }
    }

}
