namespace IncuSmart.API.Mappers
{
    public class IncubatorMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CreateIncubatorRequest, CreateIncubatorCommand>();
            config.NewConfig<UpdateConfigInstanceItemRequest, UpdateConfigInstanceItemCommand>();
        }
    }

    public class IncubatorModelMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<ModelConfigItem, ModelConfigItemCommand>();
            config.NewConfig<CreateIncubatorModelRequest, CreateIncubatorModelCommand>();
            config.NewConfig<UpdateIncubatorModelRequest, UpdateIncubatorModelCommand>();
        }
    }
}
