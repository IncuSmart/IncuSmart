namespace IncuSmart.Infra.Persistences.Repositories
{
    // ── Template ────────────────────────────────────────────────────────────────
    public class HatchingSeasonTemplateRepository : IHatchingSeasonTemplateRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public HatchingSeasonTemplateRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(HatchingSeasonTemplate template) =>
            await _dbContext.HatchingSeasonTemplates.AddAsync(template.Adapt<HatchingSeasonTemplateEntity>());

        public async Task<HatchingSeasonTemplate?> FindById(Guid id)
        {
            var entity = await _dbContext.HatchingSeasonTemplates
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<HatchingSeasonTemplate>();
        }

        // customerId null = lấy tất cả template public (TECHNICIAN) + của customer đó
        public async Task<List<HatchingSeasonTemplate>> FindAll(Guid? customerId, string? createdByType)
        {
            var query = _dbContext.HatchingSeasonTemplates
                .Where(x => x.DeletedAt == null);

            if (customerId.HasValue)
                query = query.Where(x => x.CustomerId == customerId.Value
                                      || x.CreatedByType == "TECHNICIAN");

            if (!string.IsNullOrEmpty(createdByType))
                query = query.Where(x => x.CreatedByType == createdByType);

            return (await query.OrderByDescending(x => x.CreatedAt).ToListAsync())
                .Adapt<List<HatchingSeasonTemplate>>();
        }
    }

    // ── Template Batch ──────────────────────────────────────────────────────────
    public class HatchingSeasonTemplateBatchRepository : IHatchingSeasonTemplateBatchRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public HatchingSeasonTemplateBatchRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(HatchingSeasonTemplateBatch batch) =>
            await _dbContext.HatchingSeasonTemplateBatches.AddAsync(batch.Adapt<HatchingSeasonTemplateBatchEntity>());

        public async Task<List<HatchingSeasonTemplateBatch>> FindByTemplateId(Guid templateId) =>
            (await _dbContext.HatchingSeasonTemplateBatches
                .Where(x => x.TemplateId == templateId && x.DeletedAt == null)
                .OrderBy(x => x.BatchIndex)
                .ToListAsync())
            .Adapt<List<HatchingSeasonTemplateBatch>>();

        public async Task SoftDeleteByTemplateId(Guid templateId)
        {
            var batches = await _dbContext.HatchingSeasonTemplateBatches
                .Where(x => x.TemplateId == templateId && x.DeletedAt == null)
                .ToListAsync();
            foreach (var b in batches)
            {
                b.DeletedAt = DateTime.UtcNow;
                b.DeletedBy = "SYSTEM";
                b.UpdatedAt = DateTime.UtcNow;
                b.UpdatedBy = "SYSTEM";
            }
        }
    }

    // ── Template Batch Config ────────────────────────────────────────────────────
    public class HatchingSeasonTemplateBatchConfigRepository : IHatchingSeasonTemplateBatchConfigRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public HatchingSeasonTemplateBatchConfigRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(HatchingSeasonTemplateBatchConfig config) =>
            await _dbContext.HatchingSeasonTemplateBatchConfigs.AddAsync(config.Adapt<HatchingSeasonTemplateBatchConfigEntity>());

        public async Task<List<HatchingSeasonTemplateBatchConfig>> FindByTemplateBatchId(Guid templateBatchId) =>
            (await _dbContext.HatchingSeasonTemplateBatchConfigs
                .Where(x => x.TemplateBatchId == templateBatchId && x.DeletedAt == null)
                .ToListAsync())
            .Adapt<List<HatchingSeasonTemplateBatchConfig>>();

        public async Task SoftDeleteByTemplateBatchId(Guid templateBatchId)
        {
            var configs = await _dbContext.HatchingSeasonTemplateBatchConfigs
                .Where(x => x.TemplateBatchId == templateBatchId && x.DeletedAt == null)
                .ToListAsync();
            foreach (var c in configs)
            {
                c.DeletedAt = DateTime.UtcNow;
                c.DeletedBy = "SYSTEM";
                c.UpdatedAt = DateTime.UtcNow;
                c.UpdatedBy = "SYSTEM";
            }
        }
    }
}
