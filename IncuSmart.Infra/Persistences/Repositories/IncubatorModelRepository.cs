namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorModelRepository : IIncubatorModelRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public IncubatorModelRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(IncubatorModel model) =>
            await _dbContext.IncubatorModels.AddAsync(model.Adapt<IncubatorModelEntity>());

        public async Task<IncubatorModel?> FindById(Guid id)
        {
            var entity = await _dbContext.IncubatorModels
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<IncubatorModel>();
        }

        public async Task<IncubatorModel?> FindByModelCode(string modelCode)
        {
            var entity = await _dbContext.IncubatorModels
                .FirstOrDefaultAsync(x => x.ModelCode == modelCode && x.DeletedAt == null);
            return entity?.Adapt<IncubatorModel>();
        }

        public async Task<List<IncubatorModel>> FindByIds(List<Guid> ids) =>
            (await _dbContext.IncubatorModels
                .Where(x => ids.Contains(x.Id) && x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<IncubatorModel>>();

        public async Task<List<IncubatorModel>> List(string? status = null, string? search = null)
        {
            var query = _dbContext.IncubatorModels.Where(x => x.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BaseStatus>(status, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => (x.Name != null && x.Name.Contains(search)) || (x.ModelCode != null && x.ModelCode.Contains(search)));

            return (await query.ToListAsync()).Adapt<List<IncubatorModel>>();
        }

        public async Task Update(IncubatorModel model)
        {
            var entity = await _dbContext.IncubatorModels.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (entity != null) model.Adapt(entity);
        }

        public async Task<bool> HasIncubators(Guid modelId) =>
            await _dbContext.Incubators.AnyAsync(x => x.ModelId == modelId && x.DeletedAt == null);
    }
}
