namespace IncuSmart.Core.Commands
{
    public class CreateMaintenanceLogCommand
    {
        public Guid    TicketId    { get; set; }  // set từ path param
        public string? Description { get; set; }
    }
}
