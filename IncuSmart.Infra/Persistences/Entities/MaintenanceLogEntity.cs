namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("maintenance_logs")]
    public class MaintenanceLogEntity : BaseEntity<BaseStatus>
    {
        public Guid TicketId { get; set; }
        public Guid? PerformedByUserId { get; set; }
        public string? Description { get; set; }

        [ForeignKey("TicketId")]
        public MaintenanceTicketEntity? Ticket { get; set; }

        [ForeignKey("PerformedByUserId")]
        public UserEntity? PerformedByUser { get; set; }
    }
}
