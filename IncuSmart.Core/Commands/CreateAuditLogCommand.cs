namespace IncuSmart.Core.Commands
{
    public class CreateAuditLogCommand
    {
        public Guid UserId { get; set; }
        public AuditAction Action { get; set; }
        public AuditEntityType Entity { get; set; }
        public Guid? EntityId { get; set; }
    }
}
