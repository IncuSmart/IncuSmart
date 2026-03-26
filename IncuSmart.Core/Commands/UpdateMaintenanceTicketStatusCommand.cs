namespace IncuSmart.Core.Commands
{
    public class UpdateMaintenanceTicketStatusCommand
    {
        public Guid   Id     { get; set; }  // set từ path param
        public string Status { get; set; } = string.Empty;
    }
}
