namespace IncuSmart.Core.Commands
{
    public class AssessMaintenanceConfigsCommand
    {
        public Guid TicketId { get; set; }
        public List<ConfigAssessmentItem> Items { get; set; } = [];
    }

    public class ConfigAssessmentItem
    {
        public Guid ConfigId { get; set; }
        public string Condition { get; set; } = string.Empty;
        public long MarketPrice { get; set; }
        public string? Note { get; set; }
    }
}
