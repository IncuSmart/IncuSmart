using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorModelRepository : IIncubatorModelRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public IncubatorModelRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(IncubatorModel model) =>
            await _dbContext.IncubatorModels.AddAsync(model.Adapt<IncubatorModelEntity>());


        public async Task<IncubatorModel?> FindById(Guid id)
        {
            IncubatorModelEntity? entity = await _dbContext.IncubatorModels.FirstOrDefaultAsync(x => x.Id == id);
            return entity != null ? entity.Adapt<IncubatorModel>() : null;
        }

        public async Task<List<IncubatorModel>> FindByIds(List<Guid> ids)
        {
            List<IncubatorModelEntity> entities = await _dbContext.IncubatorModels.Where(x => ids.Contains(x.Id)).ToListAsync();
            return entities.Adapt<List<IncubatorModel>>();
        }

        public async Task<List<IncubatorModel>> FindAll() =>
        (await _dbContext.IncubatorModels
            .Where(x => x.DeletedAt == null)
            .ToListAsync())
        .Adapt<List<IncubatorModel>>();

    }

}
