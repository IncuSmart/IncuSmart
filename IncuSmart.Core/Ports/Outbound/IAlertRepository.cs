using IncuSmart.Core.Domains;

namespace IncuSmart.Core.Ports.Outbound
{
    public interface IAlertRepository
    {
        Task<Alert?> FindByIdWithDetailsAsync(Guid id);
        Task<Alert> AddAsync(Alert alert);
    }
}
