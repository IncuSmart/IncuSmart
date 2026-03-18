using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class HatchingSeasonTemplate : BaseDomain<BaseStatus>
    {
        public Guid? CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TotalDays { get; set; }
        public string? EggType { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedByType { get; set; } = string.Empty;
    }

}
