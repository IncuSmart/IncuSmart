namespace IncuSmart.Core.Domains
{
    public class MaintenanceLog : BaseDomain<BaseStatus>
    {
        public Guid TicketId { get; set; }
        public Guid? PerformedByUserId { get; set; }
        public string? Description { get; set; }
    }
}
