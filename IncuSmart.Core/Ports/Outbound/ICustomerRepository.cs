using IncuSmart.Core.Domains;

namespace IncuSmart.Core.Ports.Outbound
{
    public interface ICustomerRepository
    {
        Task Add(Customer customer);

        Task<Customer?> FindById(Guid id);
        Task<Customer?> FindByUserId(Guid userId);
    }
}
