using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class TemplateBatchDetailCommand
    {
        public HatchingSeasonTemplateBatch? Batch { get; set; }
        public List<HatchingSeasonTemplateBatchConfig> Configs { get; set; } = new();
    }
}
