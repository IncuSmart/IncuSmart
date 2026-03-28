using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class MasterboardRepository : IMasterboardRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public MasterboardRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Masterboard?> FindById(Guid id)
        {
            MasterboardEntity? entity = await _dbContext.Masterboards.FirstOrDefaultAsync(m => m.Id.Equals(id));
            return entity != null ? entity.Adapt<Masterboard>() : null;
        }

        public async Task<Masterboard?> FindByIncubatorId(Guid incubatorId)
        {
            MasterboardEntity? entity = await _dbContext.Masterboards.FirstOrDefaultAsync(m => m.IncubatorId.Equals(incubatorId));
            return entity != null ? entity.Adapt<Masterboard>() : null;
        }
    }
}
