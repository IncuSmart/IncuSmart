using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class HatchingSeason : BaseDomain<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public Guid? TemplateId { get; set; }
        public string SeasonCode { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? EggType { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? TotalEggs { get; set; }
        public int SuccessCount { get; set; } = 0;
        public int FailCount { get; set; } = 0;
        public string? Notes { get; set; }
    }

}
