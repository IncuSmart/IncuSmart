using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    // ── MaintenanceTicket ────────────────────────────────────────────────────────
    public class MaintenanceTicketRepository : IMaintenanceTicketRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public MaintenanceTicketRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(MaintenanceTicket ticket) =>
            await _dbContext.MaintenanceTickets.AddAsync(ticket.Adapt<MaintenanceTicketEntity>());

        public async Task<MaintenanceTicket?> FindById(Guid id)
        {
            var entity = await _dbContext.MaintenanceTickets
                .Include(t => t.Incubator)
                .Include(t => t.Technician)
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<MaintenanceTicket>();
        }

        public async Task<List<MaintenanceTicket>> FindAll(
            Guid? incubatorId, Guid? technicianId, string? status)
        {
            var query = _dbContext.MaintenanceTickets
                .Include(t => t.Incubator)
                .Include(t => t.Technician)
                .Where(x => x.DeletedAt == null);

            if (incubatorId.HasValue)
                query = query.Where(x => x.IncubatorId == incubatorId.Value);

            if (technicianId.HasValue)
                query = query.Where(x => x.TechnicianId == technicianId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(x => x.Status.ToString() == status);

            return (await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync())
                .Adapt<List<MaintenanceTicket>>();
        }
    }

    // ── MaintenanceLog ────────────────────────────────────────────────────────────
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
