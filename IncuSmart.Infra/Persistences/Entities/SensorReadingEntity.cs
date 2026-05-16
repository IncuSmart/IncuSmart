namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("sensor_readings")]
    public class SensorReadingEntity : BaseEntity<BaseStatus>
    {
        public Guid     SensorId   { get; set; }
        public decimal? Value      { get; set; }
        public DateTime? RecordedAt { get; set; }

        // Navigation — include Sensor info + Config info (name, unit)
        [ForeignKey("SensorId")]
        public SensorEntity? Sensor { get; set; }
    }
}
