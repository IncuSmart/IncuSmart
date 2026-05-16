namespace IncuSmart.Core.Ports.Inbound
{
    public interface IAuditLogUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateAuditLogCommand command);
        Task<ResultModel<AuditLog?>> GetById(Guid id);
        Task<ResultModel<PagedResult<AuditLog>>> List(Guid? userId, string? action, string? entity, int page, int pageSize);
    }
}
