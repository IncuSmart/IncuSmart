using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class CreateIncubatorModelRequest
    {
        [Required(ErrorMessage = "ModelCode is required")]
        [MaxLength(50, ErrorMessage = "ModelCode must be at most 50 characters")]
        public string? ModelCode { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100, ErrorMessage = "Name must be at most 100 characters")]
        public string? Name { get; set; }

        [MaxLength(255, ErrorMessage = "Description must be at most 255 characters")]
        public string? Description { get; set; }

        public List<ModelConfigItem> Configs { get; set; } = [];
    }

}
