namespace IncuSmart.Core.Ports.Inbound
{
    public interface IMaintenanceTicketUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateMaintenanceTicketCommand command);
        Task<ResultModel<MaintenanceTicketDetail?>> GetById(Guid id, Guid? currentUserId, string role);
        Task<ResultModel<List<MaintenanceTicket>>> GetAll(Guid? incubatorId, Guid? technicianId, string? status, Guid? currentUserId, string role);
        Task<ResultModel<bool>> UpdateStatus(UpdateMaintenanceTicketStatusCommand command);
        Task<ResultModel<Guid?>> AddLog(CreateMaintenanceLogCommand command);
        Task<ResultModel<List<MaintenanceLog>>> GetLogs(Guid ticketId);
    }
}
