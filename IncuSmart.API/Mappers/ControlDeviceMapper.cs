namespace IncuSmart.API.Mappers
{
    public class ControlDeviceMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CreateControlDeviceRequest, CreateControlDeviceCommand>();
        }
    }
}
