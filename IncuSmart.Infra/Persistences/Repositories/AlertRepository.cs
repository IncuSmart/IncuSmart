using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public AlertRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Update(Alert alert)
        {
            var entity = await _dbContext.Alerts.FirstOrDefaultAsync(x => x.Id == alert.Id);
            if (entity != null) alert.Adapt(entity);
        }

        public async Task<Alert?> FindById(Guid id)
        {
            var entity = await _dbContext.Alerts
                .Include(a => a.Incubator)
                .Include(a => a.Config)
                .Include(a => a.Sensor)
                .Include(a => a.ResolvedMl)
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<Alert>();
        }

        public async Task<List<Alert>> List(
            Guid? incubatorId, string? severity, string? status,
            DateTime? from, DateTime? to)
        {
            var query = _dbContext.Alerts
                .Include(a => a.Incubator)
                .Include(a => a.Config)
                .Include(a => a.Sensor)
                .Where(x => x.DeletedAt == null);

            if (incubatorId.HasValue)
                query = query.Where(x => x.IncubatorId == incubatorId.Value);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(x => x.Severity == severity);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(x => x.Status.ToString() == status);

            if (from.HasValue)
                query = query.Where(x => x.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.CreatedAt <= to.Value);

            return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync())
                .Adapt<List<Alert>>();
        }

        public async Task<List<Alert>> ListByCustomer(
            Guid customerId, Guid? incubatorId, string? severity, string? status,
            DateTime? from, DateTime? to)
        {
            // Lọc alert theo các máy thuộc customer này
            var query = _dbContext.Alerts
                .Include(a => a.Incubator)
                .Include(a => a.Config)
                .Include(a => a.Sensor)
                .Where(x => x.DeletedAt == null
                         && x.Incubator != null
                         && x.Incubator.CustomerId == customerId
                         && x.Incubator.DeletedAt == null);

            if (incubatorId.HasValue)
                query = query.Where(x => x.IncubatorId == incubatorId.Value);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(x => x.Severity == severity);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(x => x.Status.ToString() == status);

            if (from.HasValue)
                query = query.Where(x => x.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.CreatedAt <= to.Value);

            return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync())
                .Adapt<List<Alert>>();
        }
    }
}
