namespace IncuSmart.Core.Ports.Outbound
{
    public interface ISensorRepository
    {
        Task Add(Sensor sensor);
        Task<List<Sensor>> FindByMasterboardId(Guid masterboardId);
        Task<List<Guid>>   FindSensorIdsByIncubatorId(Guid incubatorId);
    }
}
