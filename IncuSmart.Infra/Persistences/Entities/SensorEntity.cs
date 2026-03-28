namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("sensors")]
    public class SensorEntity : BaseEntity<BaseStatus>
    {
        public Guid    MasterboardId    { get; set; }
        public Guid    ConfigInstanceId { get; set; }
        public string? HardwareCode     { get; set; }
        public int?    PinNumber        { get; set; }

        // Navigation properties — include Config info (name, unit, type)
        [ForeignKey("MasterboardId")]
        public MasterboardEntity? Masterboard { get; set; }

        [ForeignKey("ConfigInstanceId")]
        public IncubatorConfigInstanceEntity? ConfigInstance { get; set; }
    }
}
