using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class UpdateIncubatorModelCommand
    {
        public Guid Id { get; set; }
        public string? ModelCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<ModelConfigItemCommand> Configs { get; set; } = [];
    }

}
