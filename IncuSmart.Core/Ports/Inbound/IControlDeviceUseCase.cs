using IncuSmart.Core.Commands;
using IncuSmart.Core.Domains;

namespace IncuSmart.Core.Ports.Inbound;

public interface IControlDeviceUseCase
{
    Task<ResultModel<Guid?>> Create(CreateControlDeviceCommand command);
    Task<ResultModel<IEnumerable<ControlDevice>>> GetByIncubatorId(Guid incubatorId);
}
