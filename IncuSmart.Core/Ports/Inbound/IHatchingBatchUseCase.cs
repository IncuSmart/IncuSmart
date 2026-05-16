namespace IncuSmart.Core.Ports.Inbound
{
    public interface IHatchingBatchUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateHatchingBatchCommand command, Guid? currentUserId, string role);
        Task<ResultModel<List<HatchingBatchDetailResponse>>> GetBySeasonId(Guid seasonId, Guid? currentUserId, string role);
        Task<ResultModel<bool>> Update(UpdateHatchingBatchCommand command, Guid? currentUserId, string role);
        Task<ResultModel<bool>> Delete(Guid id, Guid? currentUserId, string role);
    }
}
