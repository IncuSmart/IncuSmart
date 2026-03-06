using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class UpdateConfigCommand
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
    }

}
