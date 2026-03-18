using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("sensor_readings")]
    public class SensorReadingEntity : BaseEntity<BaseStatus>
    {
        public Guid SensorId { get; set; }
        public decimal? Value { get; set; }
        public DateTime? RecordedAt { get; set; }
    }
}
