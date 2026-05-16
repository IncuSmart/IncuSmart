namespace IncuSmart.Infra.Persistences.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AuditLogRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(AuditLog auditLog) =>
            await _dbContext.AuditLogs.AddAsync(auditLog.Adapt<AuditLogEntity>());

        public async Task<AuditLog?> FindById(Guid id)
        {
            var entity = await _dbContext.AuditLogs
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<AuditLog>();
        }

        public async Task<List<AuditLog>> List(Guid? userId, AuditAction? action, AuditEntityType? entity)
        {
            var query = _dbContext.AuditLogs.Where(x => x.DeletedAt == null);

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId.Value);

            if (action.HasValue)
                query = query.Where(x => x.Action == action.Value);

            if (entity.HasValue)
                query = query.Where(x => x.Entity == entity.Value);

            return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync()).Adapt<List<AuditLog>>();
        }
    }
}
