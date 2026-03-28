namespace IncuSmart.Infra.Persistences.Repositories
{
    public class SensorReadingRepository : ISensorReadingRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public SensorReadingRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        // Lọc theo sensorIds (thuộc incubator), sensorId, configId, from/to
        // Sắp xếp theo recorded_at DESC, giới hạn limit bản ghi
        public async Task<List<SensorReading>> FindByFilters(
            List<Guid> sensorIds,
            Guid?      sensorId,
            Guid?      configId,
            DateTime?  from,
            DateTime?  to,
            int        limit)
        {
            var query = _dbContext.SensorReadings
                .Include(r => r.Sensor)
                    .ThenInclude(s => s!.ConfigInstance)
                        .ThenInclude(ci => ci!.Config)
                .Where(x => sensorIds.Contains(x.SensorId) && x.DeletedAt == null);

            // Filter theo sensor cụ thể
            if (sensorId.HasValue)
                query = query.Where(x => x.SensorId == sensorId.Value);

            // Filter theo config (qua ConfigInstance)
            if (configId.HasValue)
                query = query.Where(x => x.Sensor != null
                                      && x.Sensor.ConfigInstance != null
                                      && x.Sensor.ConfigInstance.ConfigId == configId.Value);

            if (from.HasValue)
                query = query.Where(x => x.RecordedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.RecordedAt <= to.Value);

            // Sắp xếp DESC theo recorded_at, giới hạn số bản ghi
            return (await query
                .OrderByDescending(x => x.RecordedAt)
                .Take(limit)
                .ToListAsync())
                .Adapt<List<SensorReading>>();
        }
    }
}
