using System.ComponentModel.DataAnnotations.Schema;
using IncuSmart.Core.Enums;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("alerts")]
    public class AlertEntity : BaseEntity<BaseStatus>
    {
        [ForeignKey(nameof(Incubator))]
        public Guid IncubatorId { get; set; }
        public IncubatorEntity Incubator { get; set; }

        [ForeignKey(nameof(Config))]
        public Guid? ConfigId { get; set; }
        public IncubatorConfigInstanceEntity? Config { get; set; }


        [ForeignKey(nameof(Sensor))]
        public Guid? SensorId { get; set; }
        public SensorEntity? Sensor { get; set; }

        public decimal? Value { get; set; }
        public string? Severity { get; set; }
        public string? Message { get; set; }
        public string? ResolvedBy { get; set; }

        [ForeignKey(nameof(MlModel))]
        public Guid? ResolvedMlId { get; set; }
        public MlModelEntity? MlModel { get; set; }
    }
}
