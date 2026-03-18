using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("hatching_season_template_batches")]
    public class HatchingSeasonTemplateBatchEntity : BaseEntity<BaseStatus>
    {
        public Guid TemplateId { get; set; }
        public int BatchIndex { get; set; }
        public string? Name { get; set; }
        public int DayStart { get; set; }
        public int DayEnd { get; set; }
        public string? Notes { get; set; }
    }
}
