namespace IncuSmart.Core.Ports.Outbound
{
    public interface ISensorReadingRepository
    {
        Task<List<SensorReading>> FindByFilters(
            List<Guid>  sensorIds,
            Guid?       sensorId,
            Guid?       configId,
            DateTime?   from,
            DateTime?   to,
            int         limit);
    }
}
