namespace IncuSmart.Core.Ports.Outbound
{
    public interface IHatchingSeasonTemplateRepository
    {
        Task Add(HatchingSeasonTemplate template);
        Task<HatchingSeasonTemplate?> FindById(Guid id);
        Task<List<HatchingSeasonTemplate>> FindAll(Guid? customerId, string? createdByType);
    }

    public interface IHatchingSeasonTemplateBatchRepository
    {
        Task Add(HatchingSeasonTemplateBatch batch);
        Task<List<HatchingSeasonTemplateBatch>> FindByTemplateId(Guid templateId);
        Task SoftDeleteByTemplateId(Guid templateId);
    }

    public interface IHatchingSeasonTemplateBatchConfigRepository
    {
        Task Add(HatchingSeasonTemplateBatchConfig config);
        Task<List<HatchingSeasonTemplateBatchConfig>> FindByTemplateBatchId(Guid templateBatchId);
        Task SoftDeleteByTemplateBatchId(Guid templateBatchId);
    }
}
