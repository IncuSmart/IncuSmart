namespace IncuSmart.Infra.Persistences.Repositories
{
    public class IncubatorRepository : IIncubatorRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public IncubatorRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(Incubator incubator) =>
            await _dbContext.Incubators.AddAsync(incubator.Adapt<IncubatorEntity>());

        public async Task<bool> ExistsBySerialNumber(string serialNumber) =>
            await _dbContext.Incubators.AnyAsync(x => x.SerialNumber == serialNumber);

        public async Task<Incubator?> FindById(Guid id)
        {
            var entity = await _dbContext.Incubators
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            return entity?.Adapt<Incubator>();
        }

        public async Task<IncubatorResponse?> FindDetailById(Guid id)
        {
            return await BuildDetailQuery()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<IncubatorResponse>> List(Guid? customerId, string? status, Guid? modelId)
        {
            var query = _dbContext.Incubators.Where(x => x.DeletedAt == null);

            if (customerId.HasValue)
                query = query.Where(x => x.CustomerId == customerId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<IncubatorStatus>(status, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            if (modelId.HasValue)
                query = query.Where(x => x.ModelId == modelId);

            return await BuildDetailQuery(query).ToListAsync();
        }

        public async Task Update(Incubator incubator)
        {
            var entity = await _dbContext.Incubators.FirstOrDefaultAsync(x => x.Id == incubator.Id);
            if (entity != null) incubator.Adapt(entity);
        }

        private IQueryable<IncubatorResponse> BuildDetailQuery(IQueryable<IncubatorEntity>? source = null)
        {
            var query = source ?? _dbContext.Incubators.Where(x => x.DeletedAt == null);

            return
                from incubator in query
                join model in _dbContext.IncubatorModels.Where(x => x.DeletedAt == null)
                    on incubator.ModelId equals model.Id into models
                from model in models.DefaultIfEmpty()
                select new IncubatorResponse
                {
                    Id = incubator.Id,
                    ModelId = incubator.ModelId,
                    ModelName = model != null ? model.Name : null,
                    ModelImageUrl = model != null ? model.ImageUrl : null,
                    SerialNumber = incubator.SerialNumber,
                    CustomerId = incubator.CustomerId,
                    ActivatedAt = incubator.ActivatedAt,
                    Status = incubator.Status,
                    CreatedAt = incubator.CreatedAt,
                    CreatedBy = incubator.CreatedBy,
                    UpdatedAt = incubator.UpdatedAt,
                    UpdatedBy = incubator.UpdatedBy,
                    DeletedAt = incubator.DeletedAt,
                    DeletedBy = incubator.DeletedBy
                };
        }
    }
}
