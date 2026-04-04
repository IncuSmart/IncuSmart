using IncuSmart.Core.Domains;

namespace IncuSmart.Core.Ports.Outbound;

public interface IControlDeviceRepository
{
    Task Add(ControlDevice controlDevice);
    Task<ControlDevice?> FindById(Guid id);
    Task<IEnumerable<ControlDevice>> GetAll();
    Task<IEnumerable<ControlDevice>> GetByMasterboardId(Guid masterboardId);
}
