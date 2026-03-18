using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class HatchingSeasonTemplateBatchConfig : BaseDomain<BaseStatus>
    {
        public Guid TemplateBatchId { get; set; }
        public Guid ConfigId { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
    }

}
