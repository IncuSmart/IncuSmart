namespace IncuSmart.API.Mappers
{
    public class ConfigMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CreateConfigRequest, CreateConfigCommand>();
            config.NewConfig<UpdateConfigRequest, UpdateConfigCommand>();
        }
    }
}
