using IncuSmart.Core.Domains;

namespace IncuSmart.Core.Ports.Outbound;

public interface IControlBoardTypeRepository
{
    Task<ControlBoardType?> FindById(Guid id);
}
