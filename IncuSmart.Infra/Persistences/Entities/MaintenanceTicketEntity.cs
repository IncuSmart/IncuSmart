namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("maintenance_tickets")]
    public class MaintenanceTicketEntity : BaseEntity<MaintenanceTicketStatus>
    {
        public Guid IncubatorId { get; set; }
        public Guid? WarrantyId { get; set; }
        public Guid? RequestedByCustomerId { get; set; }
        public Guid? TechnicianId { get; set; }
        public string IssueDescription { get; set; } = string.Empty;
        public string? ResolutionSummary { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        [ForeignKey("IncubatorId")]
        public IncubatorEntity? Incubator { get; set; }

        [ForeignKey("WarrantyId")]
        public WarrantyEntity? Warranty { get; set; }

        [ForeignKey("RequestedByCustomerId")]
        public CustomerEntity? RequestedByCustomer { get; set; }

        [ForeignKey("TechnicianId")]
        public UserEntity? Technician { get; set; }
    }
}
