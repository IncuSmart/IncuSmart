namespace IncuSmart.Infra.Persistences.Repositories
{
    public class MaintenanceTicketRepository : IMaintenanceTicketRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public MaintenanceTicketRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(MaintenanceTicket ticket) =>
            await _dbContext.MaintenanceTickets.AddAsync(ticket.Adapt<MaintenanceTicketEntity>());

        public async Task Update(MaintenanceTicket ticket)
        {
            var entity = await _dbContext.MaintenanceTickets.FirstOrDefaultAsync(x => x.Id == ticket.Id);
            if (entity != null) ticket.Adapt(entity);
        }

        public async Task<MaintenanceTicket?> FindById(Guid id)
        {
            var entity = await _dbContext.MaintenanceTickets
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<MaintenanceTicket>();
        }

        public async Task<List<MaintenanceTicket>> List(
            Guid? incubatorId, Guid? technicianId, Guid? customerId, string? status)
        {
            var query = _dbContext.MaintenanceTickets
                .Where(x => x.DeletedAt == null);

            if (incubatorId.HasValue)
                query = query.Where(x => x.IncubatorId == incubatorId.Value);

            if (technicianId.HasValue)
                query = query.Where(x => x.TechnicianId == technicianId.Value);

            if (customerId.HasValue)
                query = query.Where(x => x.RequestedByCustomerId == customerId.Value);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MaintenanceTicketStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync())
                .Adapt<List<MaintenanceTicket>>();
        }
    }

    public class MaintenanceLogRepository : IMaintenanceLogRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public MaintenanceLogRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(MaintenanceLog log) =>
            await _dbContext.MaintenanceLogs.AddAsync(log.Adapt<MaintenanceLogEntity>());

        public async Task<List<MaintenanceLog>> FindByTicketId(Guid ticketId) =>
            (await _dbContext.MaintenanceLogs
                .Where(x => x.TicketId == ticketId && x.DeletedAt == null)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync())
            .Adapt<List<MaintenanceLog>>();
    }
}
