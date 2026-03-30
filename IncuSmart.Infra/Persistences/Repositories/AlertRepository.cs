using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Outbound;
using IncuSmart.Infra.Persistences.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AlertRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Alert?> FindByIdWithDetailsAsync(Guid id)
        {
            var alertEntity = await _dbContext.Set<AlertEntity>()
                .Include(a => a.Incubator)
                .Include(a => a.Config)
                .Include(a => a.Sensor)
                .Include(a => a.MlModel)
                .FirstOrDefaultAsync(a => a.Id == id);

            return alertEntity?.Adapt<Alert>();
        }

        public async Task<Alert> AddAsync(Alert alert)
        {
            var entity = alert.Adapt<AlertEntity>();
            await _dbContext.Set<AlertEntity>().AddAsync(entity);
            return entity.Adapt<Alert>();
        }

        public async Task<IEnumerable<Alert>> GetAllAsync(List<Guid>? customerIncubatorIds, Guid? incubatorId, string? severity, string? status, DateTime? from, DateTime? to)
        {
            var query = _dbContext.Set<AlertEntity>()
                .Include(a => a.Incubator)
                .Include(a => a.Config)
                .Include(a => a.Sensor)
                .Include(a => a.MlModel)
                .AsQueryable();

            if (customerIncubatorIds is not null && customerIncubatorIds.Any())
            {
                query = query.Where(a => customerIncubatorIds.Contains(a.IncubatorId));
            }

            if (incubatorId.HasValue)
            {
                query = query.Where(a => a.IncubatorId == incubatorId.Value);
            }

            if (!string.IsNullOrEmpty(severity))
            {
                query = query.Where(a => a.Severity == severity);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status.ToString() == status);
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= to.Value);
            }

            var alertEntities = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            return alertEntities.Adapt<IEnumerable<Alert>>();
        }

        public Task UpdateAsync(Alert alert)
        {
            var entity = alert.Adapt<AlertEntity>();
            _dbContext.Set<AlertEntity>().Update(entity);
            return Task.CompletedTask;
        }
    }
}
