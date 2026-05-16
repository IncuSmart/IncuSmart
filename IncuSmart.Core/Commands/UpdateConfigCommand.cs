using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class UpdateConfigCommand
    {
        public Guid    Id          { get; set; }   // set từ path param trong controller
        public string? Name        { get; set; }
        public string? Type        { get; set; }
        public string? Unit        { get; set; }
        public string? Description { get; set; }
    }
}
