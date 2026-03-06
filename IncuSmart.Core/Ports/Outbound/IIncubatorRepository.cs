namespace IncuSmart.Core.Ports.Outbound
{
    public interface IIncubatorRepository
    {
        Task Add(Incubator incubator);
    }
}
