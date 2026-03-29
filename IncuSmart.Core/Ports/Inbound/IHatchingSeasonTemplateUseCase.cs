namespace IncuSmart.Core.Ports.Inbound
{
    public interface IHatchingSeasonTemplateUseCase
    {
        Task<ResultModel<Guid?>>                          Create(CreateHatchingSeasonTemplateCommand command);
        Task<ResultModel<HatchingSeasonTemplateDetailResponse?>> GetById(Guid id);
        Task<ResultModel<List<HatchingSeasonTemplate>>>   GetAll(Guid? customerId, string? createdByType);
        Task<ResultModel<bool>>                           Update(UpdateHatchingSeasonTemplateCommand command);
    }
}
