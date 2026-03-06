using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class CreateOrderByGuestRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? Address { get; set; }
        public string? Description { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Verification pass must be at least 6 characters")]
        public string VerificationPass { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<OrderItemRequest> Items { get; set; } = [];
    }

}
