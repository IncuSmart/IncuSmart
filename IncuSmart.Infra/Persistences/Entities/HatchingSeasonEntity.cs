namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("hatching_seasons")]
    public class HatchingSeasonEntity : BaseEntity<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public Guid? TemplateId { get; set; }
        public string SeasonCode { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? EggType { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? TotalEggs { get; set; }
        public int SuccessCount { get; set; } = 0;
        public int FailCount { get; set; } = 0;
        public string? Notes { get; set; }

        [ForeignKey("IncubatorId")]
        public IncubatorEntity? Incubator { get; set; }

    }
}
