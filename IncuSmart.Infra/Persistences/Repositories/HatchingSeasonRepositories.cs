namespace IncuSmart.Infra.Persistences.Repositories
{
    // ── Season ───────────────────────────────────────────────────────────────────
    public class HatchingSeasonRepository : IHatchingSeasonRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public HatchingSeasonRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(HatchingSeason season) =>
            await _dbContext.HatchingSeasons.AddAsync(season.Adapt<HatchingSeasonEntity>());

        public async Task Update(HatchingSeason season)
        {
            var entity = await _dbContext.HatchingSeasons.FirstOrDefaultAsync(x => x.Id == season.Id);
            if (entity != null) season.Adapt(entity);
        }

        public async Task<HatchingSeason?> FindById(Guid id)
        {
            var entity = await _dbContext.HatchingSeasons
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<HatchingSeason>();
        }

        // incubatorId: filter trực tiếp
        // customerId: join qua incubators.customer_id
        public async Task<List<HatchingSeason>> List(Guid? incubatorId, Guid? customerId, string? status)
        {
            var query = _dbContext.HatchingSeasons
                .Include(s => s.Incubator)
                .Where(x => x.DeletedAt == null);

            if (incubatorId.HasValue)
                query = query.Where(x => x.IncubatorId == incubatorId.Value);

            if (customerId.HasValue)
                query = query.Where(x => x.Incubator != null
                                      && x.Incubator.CustomerId == customerId.Value
                                      && x.Incubator.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<HatchingSeasonStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync())
                .Adapt<List<HatchingSeason>>();
        }

        public async Task<List<HatchingSeason>> FindByIncubatorId(Guid incubatorId, string? status, string? eggType)
        {
            var query = _dbContext.HatchingSeasons
                .Where(x => x.IncubatorId == incubatorId && x.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<HatchingSeasonStatus>(status, out var statusEnum))
            {
                query = query.Where(x => x.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(eggType))
            {
                query = query.Where(x => x.EggType == eggType);
            }

            return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync())
                .Adapt<List<HatchingSeason>>();
        }

        public async Task<bool> ExistsByCode(string seasonCode) =>
            await _dbContext.HatchingSeasons
                .AnyAsync(x => x.SeasonCode == seasonCode && x.DeletedAt == null);
    }

    // ── Batch ────────────────────────────────────────────────────────────────────
    public class HatchingBatchRepository : IHatchingBatchRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public HatchingBatchRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(HatchingBatch batch) =>
            await _dbContext.HatchingBatches.AddAsync(batch.Adapt<HatchingBatchEntity>());

        public async Task Update(HatchingBatch batch)
        {
            var entity = await _dbContext.HatchingBatches.FirstOrDefaultAsync(x => x.Id == batch.Id);
            if (entity != null) batch.Adapt(entity);
        }

        public async Task<HatchingBatch?> FindById(Guid id)
        {
            var entity = await _dbContext.HatchingBatches
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<HatchingBatch>();
        }

        public async Task<List<HatchingBatch>> FindBySeasonId(Guid seasonId) =>
            (await _dbContext.HatchingBatches
                .Where(x => x.SeasonId == seasonId && x.DeletedAt == null)
                .OrderBy(x => x.BatchIndex)
                .ToListAsync())
            .Adapt<List<HatchingBatch>>();

        public async Task SoftDelete(Guid id, string deletedBy)
        {
            var entity = await _dbContext.HatchingBatches
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            if (entity != null)
            {
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = deletedBy;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = deletedBy;
            }
        }
    }

    // ── Batch Config ─────────────────────────────────────────────────────────────
    public class HatchingBatchConfigRepository : IHatchingBatchConfigRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public HatchingBatchConfigRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(HatchingBatchConfig config) =>
            await _dbContext.HatchingBatchConfigs.AddAsync(config.Adapt<HatchingBatchConfigEntity>());

        public async Task<List<HatchingBatchConfig>> FindByBatchId(Guid batchId) =>
            (await _dbContext.HatchingBatchConfigs
                .Where(x => x.BatchId == batchId && x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<HatchingBatchConfig>>();

        public async Task SoftDeleteByBatchId(Guid batchId)
        {
            var configs = await _dbContext.HatchingBatchConfigs
                .Where(x => x.BatchId == batchId && x.DeletedAt == null)
                .ToListAsync();
            foreach (var c in configs)
            {
                c.DeletedAt = DateTime.UtcNow;
                c.DeletedBy = CommonConst.SystemActor;
                c.UpdatedAt = DateTime.UtcNow;
                c.UpdatedBy = CommonConst.SystemActor;
            }
        }
    }
}
