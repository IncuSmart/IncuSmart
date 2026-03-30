namespace IncuSmart.Core.Ports.Inbound
{
    public interface IHatchingBatchUseCase
    {
        Task<ResultModel<Guid?>>                Create(CreateHatchingBatchCommand command);
        Task<ResultModel<List<HatchingBatchDetailCommand>>> GetBySeasonId(Guid seasonId);
        Task<ResultModel<bool>>                 Update(UpdateHatchingBatchCommand command);
    }
}
