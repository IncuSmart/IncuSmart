namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorModelConfigRepository : IIncubatorModelConfigRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public IncubatorModelConfigRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task<List<IncubatorModelConfig>> GetById(Guid modelId) =>
            (await _dbContext.IncubatorModelConfigs
                .Where(x => x.ModelId == modelId && x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<IncubatorModelConfig>>();

        public async Task<List<IncubatorModelConfig>> FindByModelId(Guid modelId) =>
            (await _dbContext.IncubatorModelConfigs
                .Where(x => x.ModelId == modelId && x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<IncubatorModelConfig>>();

        public async Task AddRange(List<IncubatorModelConfig> configs) =>
            await _dbContext.IncubatorModelConfigs.AddRangeAsync(configs.Adapt<List<IncubatorModelConfigEntity>>());

        public async Task SoftDeleteByModelId(Guid modelId)
        {
            var entities = await _dbContext.IncubatorModelConfigs
                .Where(x => x.ModelId == modelId && x.DeletedAt == null)
                .ToListAsync();

            foreach (var e in entities)
            {
                e.DeletedAt = DateTime.UtcNow;
                e.DeletedBy = CommonConst.SystemActor;
                e.UpdatedAt = DateTime.UtcNow;
                e.UpdatedBy = CommonConst.SystemActor;
            }
        }
    }
}
