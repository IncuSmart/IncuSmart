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

        // Lọc theo type (SENSOR | ACTUATOR) và status (ACTIVE | INACTIVE)
        public async Task<List<Config>> FindAll(string? type, string? status)
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

        // Kiểm tra Code đã tồn tại chưa (unique constraint)
        public async Task<bool> ExistsByCode(string code) =>
            await _dbContext.Configs
                .AnyAsync(x => x.Code == code && x.DeletedAt == null);

        // Kiểm tra config đang được tham chiếu trong incubator_model_configs
        public async Task<bool> ExistsInModelConfig(Guid configId) =>
            await _dbContext.IncubatorModelConfigs
                .AnyAsync(x => x.ConfigId == configId && x.DeletedAt == null);
    }
}
