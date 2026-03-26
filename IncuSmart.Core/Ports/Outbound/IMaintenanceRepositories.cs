namespace IncuSmart.Core.Ports.Outbound
{
    public interface IMaintenanceTicketRepository
    {
        Task Add(MaintenanceTicket ticket);
        Task<MaintenanceTicket?> FindById(Guid id);
        Task<List<MaintenanceTicket>> FindAll(Guid? incubatorId, Guid? technicianId, string? status);
    }

    public interface IMaintenanceLogRepository
    {
        Task Add(MaintenanceLog log);
        Task<List<MaintenanceLog>> FindByTicketId(Guid ticketId);
    }
}
