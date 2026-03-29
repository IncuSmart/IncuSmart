namespace IncuSmart.Core.Ports.Outbound
{
    public interface IHatchingBatchRepository
    {
        Task Add(HatchingBatch batch);
        Task<HatchingBatch?> FindById(Guid id);
        Task<List<HatchingBatch>> FindBySeasonId(Guid seasonId);
    }

    public interface IHatchingBatchConfigRepository
    {
        Task Add(HatchingBatchConfig config);
        Task<List<HatchingBatchConfig>> FindByBatchId(Guid batchId);
        Task SoftDeleteByBatchId(Guid batchId);
    }
}
