namespace IncuSmart.Core.Commands
{
    public class CreateMaintenanceTicketCommand
    {
        public Guid IncubatorId { get; set; }
        public Guid? TechnicianId { get; set; }
        public string IssueDescription { get; set; } = string.Empty;
    }
}
