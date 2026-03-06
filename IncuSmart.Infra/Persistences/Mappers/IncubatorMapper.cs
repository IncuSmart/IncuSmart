namespace IncuSmart.Infra.Persistences.Mappers
{
    public class IncubatorMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<IncubatorEntity, Incubator>();
            config.NewConfig<Incubator, IncubatorEntity>();
        }
    }
}
