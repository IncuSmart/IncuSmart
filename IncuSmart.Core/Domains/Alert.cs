namespace IncuSmart.Core.Domains
{
    public class Alert : BaseDomain<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public Incubator Incubator { get; set; }

        public Guid? ConfigId { get; set; }
        public IncubatorConfigInstance? Config { get; set; }

        public Guid? SensorId { get; set; }
        public Sensor? Sensor { get; set; }

        public decimal? Value { get; set; }
        public string? Severity { get; set; }
        public string? Message { get; set; }
        public string? ResolvedBy { get; set; }

        public Guid? ResolvedMlId { get; set; }
        public MlModel? MlModel { get; set; }
    }
}
