using IncuSmart.Core.Domains;

namespace IncuSmart.API.Responses
{
    public class MaintenanceTicketDetailResponse
    {
        public MaintenanceTicket? Ticket { get; set; }
        public List<MaintenanceLog> Logs { get; set; } = new();
    }
}
