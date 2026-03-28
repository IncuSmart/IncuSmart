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
    }
}
