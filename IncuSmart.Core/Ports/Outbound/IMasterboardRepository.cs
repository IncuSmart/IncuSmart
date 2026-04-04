using IncuSmart.Core.Domains;

namespace IncuSmart.Core.Ports.Outbound;

public interface IMasterboardRepository
{
    Task<Masterboard?> FindById(Guid id);
    Task<Masterboard?> FindByIncubatorId(Guid incubatorId);
}
