using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IncuSmart.Core.Ports.Outbound
{
    public interface IControlDeviceRepository
    {
        Task Add(ControlDevice controlDevice);
        Task<ControlDevice?> FindByHardwareCode(string hardwareCode);
        Task<ControlDevice?> FindByMasterboardIdAndPinNumber(Guid masterboardId, int pinNumber);
        Task<List<ControlDevice>> FindByMasterboardId(Guid masterboardId);
    }
}
