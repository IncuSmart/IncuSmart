namespace IncuSmart.Infra.Persistences.Repositories
{
    public class WarrantyRepository : IWarrantyRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public WarrantyRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(Warranty warranty) =>
            await _dbContext.Warranties.AddAsync(warranty.Adapt<WarrantyEntity>());

        public async Task Update(Warranty warranty)
        {
            var entity = await _dbContext.Warranties
                .FirstOrDefaultAsync(x => x.Id == warranty.Id && x.DeletedAt == null);
            if (entity != null) warranty.Adapt(entity);
        }

        public async Task<Warranty?> FindById(Guid id)
        {
            var entity = await _dbContext.Warranties
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<Warranty>();
        }

        public async Task<Warranty?> FindByIncubatorId(Guid incubatorId)
        {
            var entity = await _dbContext.Warranties
                .FirstOrDefaultAsync(x => x.IncubatorId == incubatorId && x.DeletedAt == null);
            return entity?.Adapt<Warranty>();
        }
    }

}
