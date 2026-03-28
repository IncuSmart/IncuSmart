using Mapster;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorConfigInstanceRepository : IIncubatorConfigInstanceRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public IncubatorConfigInstanceRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task AddRange(List<IncubatorConfigInstance> incubatorConfigInstances)
        {
            var entities = incubatorConfigInstances.Adapt<List<IncubatorConfigInstanceEntity>>();
            await _dbContext.AddRangeAsync(entities);
        }

        public async Task<IncubatorConfigInstance?> FindById(Guid id)
        {
            var entity = await _dbContext.IncubatorConfigInstances
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<IncubatorConfigInstance>();
        }
    }
}
