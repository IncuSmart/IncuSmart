
namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorRepository : IIncubatorRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public IncubatorRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(Incubator incubator)
        {
            IncubatorEntity entity = incubator.Adapt<IncubatorEntity>();
            await _dbContext.AddAsync(entity);
        }
    }
}
