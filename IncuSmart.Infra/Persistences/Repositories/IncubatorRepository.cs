using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Outbound;
using IncuSmart.Infra.Persistences.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorRepository : IIncubatorRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public IncubatorRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(Incubator incubator)
        {
            IncubatorEntity entity = incubator.Adapt<IncubatorEntity>();
            await _dbContext.AddAsync(entity);
        }

        public async Task<IEnumerable<Incubator>> FindByCustomerId(Guid customerId)
        {
            var entities = await _dbContext.Incubators
                .Where(i => i.CustomerId == customerId)
                .ToListAsync();
            return entities.Adapt<IEnumerable<Incubator>>();
        }
    }
}
