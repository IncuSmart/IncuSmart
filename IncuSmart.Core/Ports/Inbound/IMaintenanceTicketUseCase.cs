namespace IncuSmart.Core.Ports.Inbound
{
    public interface IMaintenanceTicketUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateMaintenanceTicketCommand command, Guid? currentUserId, string role);
        Task<ResultModel<MaintenanceTicketDetailResponse?>> GetById(Guid id, Guid? currentUserId, string role);
        Task<ResultModel<PagedResult<MaintenanceTicket>>> List(Guid? incubatorId, Guid? technicianId, string? status, Guid? currentUserId, string role, int page, int pageSize);
        Task<ResultModel<bool>> Assign(AssignMaintenanceTicketCommand command, Guid? currentUserId, string role);
        Task<ResultModel<bool>> UpdateStatus(UpdateMaintenanceTicketStatusCommand command, Guid? currentUserId, string role);
        Task<ResultModel<bool>> Cancel(Guid id, Guid? currentUserId, string role);
        Task<ResultModel<Guid?>> AddLog(CreateMaintenanceLogCommand command, Guid? currentUserId, string role);
        Task<ResultModel<List<MaintenanceLog>>> GetLogs(Guid ticketId, Guid? currentUserId, string role);
        Task<ResultModel<MaintenanceTicketPaymentResponse?>> AssessConfigs(AssessMaintenanceConfigsCommand command, Guid? currentUserId, string role);
        Task<ResultModel<bool>> HandlePaymentWebhook(HandleOrderPaymentWebhookCommand command);
    }
}
