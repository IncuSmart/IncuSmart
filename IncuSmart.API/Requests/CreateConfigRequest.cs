using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class CreateConfigRequest
    {
        [Required(ErrorMessage = "Code is required")]
        [MinLength(2, ErrorMessage = "Code must be at least 2 characters")]
        [MaxLength(50, ErrorMessage = "Code must be at most 50 characters")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
        [MaxLength(100, ErrorMessage = "Name must be at most 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Type must be at most 50 characters")]
        public string? Type { get; set; }

        [MaxLength(20, ErrorMessage = "Unit must be at most 20 characters")]
        public string? Unit { get; set; }

        [MaxLength(255, ErrorMessage = "Description must be at most 255 characters")]
        public string? Description { get; set; }
    }
}
