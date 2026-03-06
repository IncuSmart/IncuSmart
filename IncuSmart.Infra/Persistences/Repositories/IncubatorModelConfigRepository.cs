using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorModelConfigRepository : IIncubatorModelConfigRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public IncubatorModelConfigRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Add(IncubatorModelConfig config)
        {
            IncubatorModelConfigEntity entity = config.Adapt<IncubatorModelConfigEntity>();
            await _dbContext.AddAsync(entity);
        }

        public async Task<List<IncubatorModelConfig>> GetById(Guid id)
        {
            List<IncubatorModelConfigEntity> listEntities = await _dbContext.IncubatorModelConfigs.Where(x => x.Id == id).ToListAsync();
            return listEntities.Adapt<List<IncubatorModelConfig>>();
        }

        public async Task AddRange(List<IncubatorModelConfig> configs) =>
    await _dbContext.IncubatorModelConfigs.AddRangeAsync(configs.Adapt<List<IncubatorModelConfigEntity>>());

        public async Task<List<IncubatorModelConfig>> FindByModelId(Guid modelId) =>
            (await _dbContext.IncubatorModelConfigs
                .Where(x => x.ModelId == modelId && x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<IncubatorModelConfig>>();

        public async Task DeleteByModelId(Guid modelId)
        {
            var entities = await _dbContext.IncubatorModelConfigs
                .Where(x => x.ModelId == modelId && x.DeletedAt == null)
                .ToListAsync();

            foreach (var e in entities)
            {
                e.DeletedAt = DateTime.UtcNow;
                e.DeletedBy = "SYSTEM";
                e.UpdatedAt = DateTime.UtcNow;
                e.UpdatedBy = "SYSTEM";
            }

        }
    }
        
}
