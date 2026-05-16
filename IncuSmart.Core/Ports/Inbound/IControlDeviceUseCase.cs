using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IncuSmart.Core.Ports.Inbound
{
    public interface IControlDeviceUseCase
    {
        Task<ResultModel<Guid?>>              Create(CreateControlDeviceCommand command);
        Task<ResultModel<List<ControlDevice>>> GetByIncubatorId(Guid incubatorId, Guid? currentUserId, string role);
    }
}
