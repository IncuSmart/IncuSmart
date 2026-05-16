namespace IncuSmart.Core.Ports.Outbound
{
    public interface IMaintenanceTicketRepository
    {
        Task Add(MaintenanceTicket ticket);
        Task Update(MaintenanceTicket ticket);
        Task<MaintenanceTicket?> FindById(Guid id);
        Task<List<MaintenanceTicket>> List(Guid? incubatorId, Guid? technicianId, Guid? customerId, string? status);
    }

    public interface IMaintenanceLogRepository
    {
        Task Add(MaintenanceLog log);
        Task<List<MaintenanceLog>> FindByTicketId(Guid ticketId);
    }
}
