namespace IncuSmart.Core.Ports.Inbound
{
    public interface IDeviceCommandUseCase
    {
        Task<ResultModel<bool>> SetPower(Guid incubatorId, bool on);
        Task<ResultModel<bool>> SetHeaterMode(Guid incubatorId, string mode);
        Task<ResultModel<bool>> SetFanMode(Guid incubatorId, string mode);
        Task<ResultModel<bool>> SetFanSpeed(Guid incubatorId, int speed);
        Task<ResultModel<bool>> SetTemperature(Guid incubatorId, double value);
        Task<ResultModel<bool>> TurnTray(Guid incubatorId);
        Task<ResultModel<bool>> StopAutoTurn(Guid incubatorId);
        Task<ResultModel<bool>> StartAutoTurn(Guid incubatorId);
        Task<ResultModel<bool>> StartIncubation(Guid incubatorId);
        Task<ResultModel<bool>> ResetIncubation(Guid incubatorId);
        Task<ResultModel<bool>> DisableFanCheck(Guid incubatorId);
        Task<ResultModel<bool>> EnableFanCheck(Guid incubatorId);
        Task<ResultModel<bool>> ResetWifi(Guid incubatorId);
        Task<ResultModel<bool>> Reboot(Guid incubatorId);
    }
}
