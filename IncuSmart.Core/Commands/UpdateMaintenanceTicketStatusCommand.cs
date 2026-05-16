namespace IncuSmart.Core.Commands
{
    public class UpdateMaintenanceTicketStatusCommand
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ResolutionSummary { get; set; }
    }
}
