namespace IncuSmart.Core.Ports.Outbound
{
    public interface IAuditLogRepository
    {
        Task Add(AuditLog auditLog);
        Task<AuditLog?> FindById(Guid id);
        Task<List<AuditLog>> List(Guid? userId, AuditAction? action, AuditEntityType? entity);
    }
}
