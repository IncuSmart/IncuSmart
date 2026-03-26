using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class WarrantyRepository : IWarrantyRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public WarrantyRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(Warranty warranty) =>
            await _dbContext.Warranties.AddAsync(warranty.Adapt<WarrantyEntity>());

        public async Task<Warranty?> FindByIncubatorId(Guid incubatorId)
        {
            var entity = await _dbContext.Warranties
                .FirstOrDefaultAsync(x => x.IncubatorId == incubatorId && x.DeletedAt == null);
            return entity?.Adapt<Warranty>();
        }
    }
}
