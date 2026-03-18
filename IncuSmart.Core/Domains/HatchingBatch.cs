using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class HatchingBatch : BaseDomain<BaseStatus>
    {
        public Guid SeasonId { get; set; }
        public int BatchIndex { get; set; }
        public string? Name { get; set; }
        public int DayStart { get; set; }
        public int DayEnd { get; set; }
        public DateTime? ActualStartAt { get; set; }
        public DateTime? ActualEndAt { get; set; }
    }

}
