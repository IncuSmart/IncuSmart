using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("ml_models")]
    public class MlModelEntity : BaseEntity<BaseStatus>
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? Config { get; set; }
        public bool? Active { get; set; }
    }
}
