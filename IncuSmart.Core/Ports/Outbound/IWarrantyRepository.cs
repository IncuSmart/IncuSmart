namespace IncuSmart.Core.Ports.Outbound
{
    public interface IWarrantyRepository
    {
        Task Add(Warranty warranty);
        Task Update(Warranty warranty);
        Task<Warranty?> FindById(Guid id);
        Task<Warranty?> FindByIncubatorId(Guid incubatorId);
    }
}
