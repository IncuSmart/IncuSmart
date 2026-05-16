
namespace IncuSmart.Core.Ports.Outbound
{
    public interface IIncubatorModelRepository
    {
        Task<IncubatorModel?> FindById(Guid id);
        Task<IncubatorModel?> FindByModelCode(string modelCode);
        Task<List<IncubatorModel>> FindByIds(List<Guid> ids);
        Task Add(IncubatorModel model);
        Task<List<IncubatorModel>> List(string? status = null, string? search = null);
        Task Update(IncubatorModel model);
        Task<bool> HasIncubators(Guid modelId);
    }
}
