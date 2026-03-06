using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<List<Config>> FindAll() =>
            (await _dbContext.Configs
                .Where(x => x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<Config>>();

        public async Task<bool> ExistsInModelConfig(Guid configId) =>
            await _dbContext.IncubatorModelConfigs
                .AnyAsync(x => x.ConfigId == configId && x.DeletedAt == null);
    }

}
