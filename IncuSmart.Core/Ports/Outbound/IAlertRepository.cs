using IncuSmart.Core.Domains;

namespace IncuSmart.Core.Ports.Outbound
{
    public interface IAlertRepository
    {
        Task<Alert?> FindByIdWithDetailsAsync(Guid id);
        Task<Alert> AddAsync(Alert alert);
        Task<IEnumerable<Alert>> GetAllAsync(List<Guid>? customerIncubatorIds, Guid? incubatorId, string? severity, string? status, DateTime? from, DateTime? to);
        Task UpdateAsync(Alert alert);
    }
}
