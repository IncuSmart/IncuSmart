namespace IncuSmart.Core.Ports.Inbound
{
    public interface IDeviceCommandUseCase
    {
        Task<ResultModel<bool>> SetPower(Guid incubatorId, bool on);
        Task<ResultModel<bool>> SetHeaterMode(Guid incubatorId, string mode);
        Task<ResultModel<bool>> SetFanMode(Guid incubatorId, string mode);
    }
}
