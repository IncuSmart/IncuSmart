namespace IncuSmart.Core.Ports.Outbound
{
    public interface IWarrantyRepository
    {
        Task Add(Warranty warranty);
        Task<Warranty?> FindByIncubatorId(Guid incubatorId);
    }
}
