
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
        public async Task<Incubator?> FindById(Guid id)
        {
            var entity = await _dbContext.Incubators
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<Incubator>();
        }

        public async Task<Incubator?> FindByQrCode(string qrCode)
        {
            var entity = await _dbContext.Incubators
                .FirstOrDefaultAsync(x => x.QrCode == qrCode && x.DeletedAt == null);
            return entity?.Adapt<Incubator>();
        }

        public async Task<List<Incubator>> FindAll() =>
            (await _dbContext.Incubators
                .Where(x => x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<Incubator>>();

        public async Task<List<Incubator>> FindByCustomerId(Guid customerId) =>
            (await _dbContext.Incubators
                .Where(x => x.CustomerId == customerId && x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<Incubator>>();
    }
}

