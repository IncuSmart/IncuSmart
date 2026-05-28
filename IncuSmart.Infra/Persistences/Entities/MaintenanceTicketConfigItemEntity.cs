namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("maintenance_ticket_config_items")]
    public class MaintenanceTicketConfigItemEntity : BaseEntity<BaseStatus>
    {
        public Guid TicketId { get; set; }
        public Guid ConfigId { get; set; }
        public string Condition { get; set; } = string.Empty;
        public long MarketPrice { get; set; }
        public long FinalPrice { get; set; }
        public string? Note { get; set; }

        [ForeignKey("TicketId")]
        public MaintenanceTicketEntity? Ticket { get; set; }

        [ForeignKey("ConfigId")]
        public ConfigEntity? Config { get; set; }
    }
}
