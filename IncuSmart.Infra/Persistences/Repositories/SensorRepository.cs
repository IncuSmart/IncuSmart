namespace IncuSmart.Infra.Persistences.Repositories
{
    public class SensorRepository : ISensorRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public SensorRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(Sensor sensor) =>
            await _dbContext.Sensors.AddAsync(sensor.Adapt<SensorEntity>());

        public async Task<Sensor?> FindByHardwareCode(string hardwareCode)
        {
            var entity = await _dbContext.Sensors
                .FirstOrDefaultAsync(x => x.HardwareCode == hardwareCode && x.DeletedAt == null);
            return entity?.Adapt<Sensor>();
        }

        public async Task<Sensor?> FindByMasterboardIdAndPinNumber(Guid masterboardId, int pinNumber)
        {
            var entity = await _dbContext.Sensors
                .FirstOrDefaultAsync(x => x.MasterboardId == masterboardId && x.PinNumber == pinNumber && x.DeletedAt == null);
            return entity?.Adapt<Sensor>();
        }

        // Include ConfigInstance → Config để có name, unit, type
        public async Task<List<Sensor>> FindByMasterboardId(Guid masterboardId)
        {
            return (await _dbContext.Sensors
                .Include(s => s.ConfigInstance)
                    .ThenInclude(ci => ci!.Config)
                .Where(x => x.MasterboardId == masterboardId && x.DeletedAt == null)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync())
                .Adapt<List<Sensor>>();
        }

        // Lấy tất cả sensorId thuộc incubator để filter readings
        public async Task<List<Guid>> FindSensorIdsByIncubatorId(Guid incubatorId)
        {
            return await _dbContext.Sensors
                .Include(s => s.Masterboard)
                .Where(x => x.Masterboard != null
                         && x.Masterboard.IncubatorId == incubatorId
                         && x.Masterboard.DeletedAt == null
                         && x.DeletedAt == null)
                .Select(x => x.Id)
                .ToListAsync();
        }
    }
}
