namespace IncuSmart.Core.Ports.Outbound
{
    public interface IMaintenanceTicketRepository
    {
        Task Add(MaintenanceTicket ticket);
        Task Update(MaintenanceTicket ticket);
        Task<MaintenanceTicket?> FindById(Guid id);
        Task<MaintenanceTicket?> FindByPaymentOrderCode(long paymentOrderCode);
        Task<List<MaintenanceTicket>> List(Guid? incubatorId, Guid? technicianId, Guid? customerId, string? status);
    }

    public interface IMaintenanceLogRepository
    {
        Task Add(MaintenanceLog log);
        Task<List<MaintenanceLog>> FindByTicketId(Guid ticketId);
    }

    public interface IMaintenanceTicketConfigItemRepository
    {
        Task AddRange(List<MaintenanceTicketConfigItem> items);
        Task DeleteByTicketId(Guid ticketId);
        Task<List<MaintenanceTicketConfigItem>> FindByTicketId(Guid ticketId);
    }
}
