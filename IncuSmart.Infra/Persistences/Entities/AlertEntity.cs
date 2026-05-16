using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("alerts")]
    public class AlertEntity : BaseEntity<AlertStatus>
    {
        public Guid IncubatorId { get; set; }
        public Guid? ConfigId { get; set; }
        public Guid? SensorId { get; set; }
        public decimal? Value { get; set; }
        public string? Severity { get; set; }
        public string? Message { get; set; }
        public AlertResolvedBy? ResolvedBy { get; set; }
        public Guid? ResolvedMlId { get; set; }

        [ForeignKey("IncubatorId")]
        public IncubatorEntity? Incubator { get; set; }

        [ForeignKey("ConfigId")]
        public ConfigEntity? Config { get; set; }

        [ForeignKey("SensorId")]
        public SensorEntity? Sensor { get; set; }

        [ForeignKey("ResolvedMlId")]
        public MlModelEntity? ResolvedMl { get; set; }
    }
}
