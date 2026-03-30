using IncuSmart.Core.Domains;

namespace IncuSmart.Core.Ports.Outbound
{
    public interface IIncubatorRepository
    {
        Task Add(Incubator incubator);
        Task<IEnumerable<Incubator>> FindByCustomerId(Guid customerId);
    }
}
