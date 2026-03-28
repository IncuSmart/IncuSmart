namespace IncuSmart.Core.Ports.Outbound
{
    public interface IIncubatorConfigInstanceRepository
    {
        Task AddRange(List<IncubatorConfigInstance> incubatorConfigInstances);
        Task<IncubatorConfigInstance?> FindById(Guid id);
    }
}
