using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class ModelConfigItemCommand
    {
        public Guid ConfigId { get; set; }
        public int? Quantity { get; set; }
        public bool? Required { get; set; }
    }

}
