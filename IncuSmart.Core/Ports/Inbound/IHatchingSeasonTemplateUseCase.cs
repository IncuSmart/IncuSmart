namespace IncuSmart.Core.Ports.Inbound
{
    public interface IHatchingSeasonTemplateUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateHatchingSeasonTemplateCommand command, Guid? currentUserId, string role);
        Task<ResultModel<HatchingSeasonTemplateDetailResponse?>> GetById(Guid id, Guid? currentUserId, string role);
        Task<ResultModel<PagedResult<HatchingSeasonTemplate>>> List(Guid? customerId, string? createdByType, Guid? currentUserId, string role, int page, int pageSize);
        Task<ResultModel<bool>> Update(UpdateHatchingSeasonTemplateCommand command, Guid? currentUserId, string role);
        Task<ResultModel<bool>> Delete(Guid id, Guid? currentUserId, string role);
    }
}
