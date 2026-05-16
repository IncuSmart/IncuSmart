namespace IncuSmart.Infra.Persistences.Repositories
{
    public class ConfigRepository : IConfigRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public ConfigRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(Config config) =>
            await _dbContext.Configs.AddAsync(config.Adapt<ConfigEntity>());

        public async Task<Config?> FindById(Guid id)
        {
            var entity = await _dbContext.Configs
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<Config>();
        }

        public async Task<List<Config>> List(string? type, string? status)
        {
            var query = _dbContext.Configs
                .Where(x => x.DeletedAt == null);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(x => x.Type == type);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(x => x.Status.ToString() == status);

            return (await query
                    .OrderBy(x => x.Code)
                    .ToListAsync())
                .Adapt<List<Config>>();
        }

        public async Task<List<Config>> FindByIds(List<Guid> ids) =>
            (await _dbContext.Configs
                .Where(x => ids.Contains(x.Id) && x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<Config>>();

        public async Task<bool> ExistsByCode(string code) =>
            await _dbContext.Configs
                .AnyAsync(x => x.Code == code && x.DeletedAt == null);

        public async Task<bool> ExistsInModelConfig(Guid configId) =>
            await _dbContext.IncubatorModelConfigs
                .AnyAsync(x => x.ConfigId == configId && x.DeletedAt == null);
    }
}
