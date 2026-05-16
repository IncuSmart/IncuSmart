namespace IncuSmart.Core.Ports.Outbound
{
    public interface IHatchingBatchRepository
    {
        Task Add(HatchingBatch batch);
        Task Update(HatchingBatch batch);
        Task<HatchingBatch?> FindById(Guid id);
        Task<List<HatchingBatch>> FindBySeasonId(Guid seasonId);
        Task SoftDelete(Guid id, string deletedBy);
    }

    public interface IHatchingBatchConfigRepository
    {
        Task Add(HatchingBatchConfig config);
        Task<List<HatchingBatchConfig>> FindByBatchId(Guid batchId);
        Task SoftDeleteByBatchId(Guid batchId);
    }
}
