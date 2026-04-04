using IncuSmart.API.Requests;
using IncuSmart.Core.Commands;
using Mapster;

namespace IncuSmart.API.Mappers;

public class ControlDeviceMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateControlDeviceRequest, CreateControlDeviceCommand>();
    }
}
