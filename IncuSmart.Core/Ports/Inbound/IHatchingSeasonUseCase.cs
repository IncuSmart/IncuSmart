namespace IncuSmart.Core.Ports.Inbound
{
    public interface IHatchingSeasonUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateHatchingSeasonCommand command, Guid? currentUserId, string role);
        Task<ResultModel<HatchingSeasonDetailResponse?>> GetById(Guid id, Guid? currentUserId, string role);
        Task<ResultModel<PagedResult<HatchingSeason>>> List(Guid? incubatorId, Guid? filterCustomerId, string? status, Guid? currentUserId, string role, int page, int pageSize);
        Task<ResultModel<bool>> Update(UpdateHatchingSeasonCommand command, Guid? currentUserId, string role);
        Task<ResultModel<bool>> UpdateStatus(UpdateHatchingSeasonStatusCommand command, Guid? currentUserId, string role);
    }
}
