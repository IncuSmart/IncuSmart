
using Mapster;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorConfigInstanceRepository : IIncubatorConfigInstanceRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public IncubatorConfigInstanceRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddRange(List<IncubatorConfigInstance> incubatorConfigInstances)
        {
            List<IncubatorConfigInstanceEntity> entities = incubatorConfigInstances.Adapt<List<IncubatorConfigInstanceEntity>>();
            await _dbContext.AddRangeAsync(entities);
        }
    }
}
