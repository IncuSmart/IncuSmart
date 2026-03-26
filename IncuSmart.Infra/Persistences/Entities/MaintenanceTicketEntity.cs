namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("maintenance_tickets")]
    public class MaintenanceTicketEntity : BaseEntity<BaseStatus>
    {
        public Guid      IncubatorId  { get; set; }
        public Guid      TechnicianId { get; set; }
        public DateTime? ClosedAt     { get; set; }

        [ForeignKey("IncubatorId")]
        public IncubatorEntity? Incubator { get; set; }

        [ForeignKey("TechnicianId")]
        public UserEntity? Technician { get; set; }
    }
}
