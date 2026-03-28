namespace IncuSmart.Core.Ports.Inbound
{
    public interface ISensorReadingUseCase
    {
        Task<ResultModel<List<SensorReading>>> GetByFilters(
            Guid        incubatorId,
            Guid?       sensorId,
            Guid?       configId,
            DateTime?   from,
            DateTime?   to,
            int         limit,
            Guid?       currentUserId,
            string      role);
    }
}
