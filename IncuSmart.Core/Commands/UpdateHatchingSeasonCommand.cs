namespace IncuSmart.Core.Commands
{
    public class UpdateHatchingSeasonCommand
    {
        public Guid Id { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? TotalEggs { get; set; }
        public int? SuccessCount { get; set; }
        public int? FailCount { get; set; }
        public string? Notes { get; set; }
    }
}
