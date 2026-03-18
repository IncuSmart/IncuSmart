using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("hatching_season_template_batch_configs")]
    public class HatchingSeasonTemplateBatchConfigEntity : BaseEntity<BaseStatus>
    {
        public Guid TemplateBatchId { get; set; }
        public Guid ConfigId { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
    }
}
