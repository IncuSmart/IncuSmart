namespace IncuSmart.API.Mappers
{
    public class SensorMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CreateSensorRequest, CreateSensorCommand>();
        }
    }
}
