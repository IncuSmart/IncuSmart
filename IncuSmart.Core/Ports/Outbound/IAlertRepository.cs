namespace IncuSmart.Core.Ports.Outbound
{
    public interface IAlertRepository
    {
        Task Update(Alert alert);
        Task<Alert?> FindById(Guid id);
        Task<List<Alert>> List(Guid? incubatorId, string? severity, string? status, DateTime? from, DateTime? to);
        Task<List<Alert>> ListByCustomer(Guid customerId, Guid? incubatorId, string? severity, string? status, DateTime? from, DateTime? to);
    }
}
