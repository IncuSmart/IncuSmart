namespace IncuSmart.Core.Ports.Inbound
{
    public interface ISensorUseCase
    {
        Task<ResultModel<Guid?>>         Create(CreateSensorCommand command);
        Task<ResultModel<List<Sensor>>>  GetByIncubatorId(Guid incubatorId, Guid? currentUserId, string role);
    }
}
