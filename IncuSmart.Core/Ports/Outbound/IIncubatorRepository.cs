namespace IncuSmart.Core.Ports.Outbound
{
    public interface IIncubatorRepository
    {
        Task Add(Incubator incubator);
        Task<Incubator?> FindById(Guid id);
        Task<IncubatorResponse?> FindDetailById(Guid id);
        Task<List<IncubatorResponse>> List(Guid? customerId, string? status, Guid? modelId);
        Task Update(Incubator incubator);
    }
}
