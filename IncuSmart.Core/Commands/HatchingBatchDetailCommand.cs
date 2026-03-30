using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class HatchingBatchDetailCommand
    {
        public HatchingBatch? Batch { get; set; }
        public List<HatchingBatchConfig> Configs { get; set; } = new();
    }
}
