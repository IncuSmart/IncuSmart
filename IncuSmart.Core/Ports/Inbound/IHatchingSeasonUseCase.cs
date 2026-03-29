namespace IncuSmart.Core.Ports.Inbound
{
    public interface IHatchingSeasonUseCase
    {
        Task<ResultModel<Guid?>>              Create(CreateHatchingSeasonCommand command);
        Task<ResultModel<List<HatchingSeason>>> GetAll(Guid? incubatorId, Guid? customerId, string role);
        Task<ResultModel<bool>>               Update(UpdateHatchingSeasonCommand command);
    }
}
