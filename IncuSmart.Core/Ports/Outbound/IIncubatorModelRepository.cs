
namespace IncuSmart.Core.Ports.Outbound
{
    public interface IIncubatorModelRepository
    {
        Task<IncubatorModel?> FindById(Guid id);
        Task<List<IncubatorModel>> FindByIds(List<Guid> ids);
        Task Add(IncubatorModel model);
        Task<List<IncubatorModel>> FindAll();
    }
}
