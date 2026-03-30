using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class HatchingSeasonTemplateDetailCommand
    {
        public HatchingSeasonTemplate? Template { get; set; }
        public List<TemplateBatchDetailCommand> Batches { get; set; } = new();
    }
}
