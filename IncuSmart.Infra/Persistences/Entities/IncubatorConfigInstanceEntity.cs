namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("incubator_config_instances")]
    public class IncubatorConfigInstanceEntity : BaseEntity<BaseStatus>
    {
        public Guid     IncubatorId    { get; set; }
        public Guid     ConfigId       { get; set; }
        public int      InstanceIndex  { get; set; }
        public decimal? TargetValue    { get; set; }
        public decimal? MinValue       { get; set; }
        public decimal? MaxValue       { get; set; }

        // Navigation — để .Include() lấy Config info (name, unit, type)
        [ForeignKey("ConfigId")]
        public ConfigEntity? Config { get; set; }
    }
}
