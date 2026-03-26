namespace IncuSmart.Core.Ports.Outbound
{
    public interface IIncubatorRepository
    {
        Task Add(Incubator incubator);
        Task<Incubator?> FindById(Guid id);
        Task<List<Incubator>> FindAll();
        Task<List<Incubator>> FindByCustomerId(Guid customerId);
    }
}
