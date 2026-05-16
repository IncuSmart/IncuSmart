namespace IncuSmart.Core.Ports.Inbound
{
    public interface IAlertUseCase
    {
        Task<ResultModel<Alert?>> GetById(Guid id, Guid? currentUserId, string role);
        Task<ResultModel<PagedResult<Alert>>> List(Guid? incubatorId, string? severity, string? status, DateTime? from, DateTime? to, Guid? currentUserId, string role, int page, int pageSize);
        Task<ResultModel<bool>> Resolve(ResolveAlertCommand command);
    }
}
