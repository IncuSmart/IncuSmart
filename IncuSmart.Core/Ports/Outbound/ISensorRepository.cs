namespace IncuSmart.Core.Ports.Outbound
{
    public interface ISensorRepository
    {
        Task Add(Sensor sensor);
        Task<Sensor?> FindByHardwareCode(string hardwareCode);
        Task<Sensor?> FindByMasterboardIdAndPinNumber(Guid masterboardId, int pinNumber);
        Task<List<Sensor>> FindByMasterboardId(Guid masterboardId);
        Task<List<Guid>>   FindSensorIdsByIncubatorId(Guid incubatorId);
    }
}
