namespace IncuSmart.Core.Domains
{
    public class MaintenanceTicketConfigItem : BaseDomain<BaseStatus>
    {
        public Guid TicketId { get; set; }
        public Guid ConfigId { get; set; }
        public string Condition { get; set; } = string.Empty;
        public long MarketPrice { get; set; }
        public long FinalPrice { get; set; }
        public string? Note { get; set; }
    }
}
