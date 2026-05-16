namespace IncuSmart.Core.Ports.Inbound
{
    public interface IIncubatorUseCase
    {
        Task<ResultModel<List<Guid>>> Create(CreateIncubatorCommand command);
        Task<ResultModel<IncubatorResponse?>> GetById(Guid id, Guid requesterId, string requesterRole);
        Task<ResultModel<PagedResult<IncubatorResponse>>> List(Guid requesterId, string requesterRole, string? status, Guid? modelId, int page, int pageSize);
        Task<ResultModel<bool>> UpdateConfigInstances(UpdateConfigInstancesCommand command);
        Task<ResultModel<bool>> UpdateStatus(Guid id, string status, string updatedBy);
        Task<ResultModel<List<HatchingSeason>>> GetHatchingSeasons(Guid incubatorId, Guid requesterId, string requesterRole, string? status, string? eggType);
    }
}
