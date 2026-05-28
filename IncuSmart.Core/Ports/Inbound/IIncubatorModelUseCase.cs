namespace IncuSmart.Core.Ports.Inbound
{
    public interface IIncubatorModelUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateIncubatorModelCommand command);
        Task<ResultModel<IncubatorModel?>> GetById(Guid id);
        Task<ResultModel<List<ModelConfigWithDetail>>> GetConfigs(Guid modelId);
        Task<ResultModel<PagedResult<IncubatorModel>>> List(string? status, string? search, int page, int pageSize);
        Task<ResultModel<bool>> Update(UpdateIncubatorModelCommand command);
        Task<ResultModel<bool>> Delete(Guid id);
    }

}
