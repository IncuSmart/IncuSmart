using System;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class MasterboardRepository : IMasterboardRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public MasterboardRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task<Masterboard?> FindById(Guid id)
        {
            var entity = await _dbContext.Masterboards
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<Masterboard>();
        }

        public async Task<Masterboard?> FindByIncubatorId(Guid incubatorId)
        {
            var entity = await _dbContext.Masterboards
                .FirstOrDefaultAsync(x => x.IncubatorId == incubatorId && x.DeletedAt == null);
            return entity?.Adapt<Masterboard>();
        }

        public async Task<Masterboard?> FindByMacAddress(string macAddress)
        {
            var normalized = macAddress.Replace(":", "").ToLower();

            // Load active boards into memory then normalize for comparison
            // (avoids EF translation issues with Replace/ToLower on PostgreSQL)
            var boards = await _dbContext.Masterboards
                .Where(x => x.DeletedAt == null && x.MacAddress != null)
                .ToListAsync();

            var entity = boards.FirstOrDefault(x =>
                x.MacAddress!.Replace(":", "").ToLower() == normalized);

            return entity?.Adapt<Masterboard>();
        }

        public async Task UpdateLastSeenAt(Guid id, DateTime lastSeenAt)
        {
            var entity = await _dbContext.Masterboards
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            if (entity == null) return;

            entity.LastSeenAt = lastSeenAt;
            await _dbContext.SaveChangesAsync();
        }
    }
}
