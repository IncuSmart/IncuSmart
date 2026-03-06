namespace IncuSmart.Core.Ports.Outbound
{
    public interface IIncubatorModelConfigRepository
    {
        Task<List<IncubatorModelConfig>> GetById(Guid id);
        Task AddRange(List<IncubatorModelConfig> configs);
        Task<List<IncubatorModelConfig>> FindByModelId(Guid modelId);
        Task DeleteByModelId(Guid modelId);

    }
}
