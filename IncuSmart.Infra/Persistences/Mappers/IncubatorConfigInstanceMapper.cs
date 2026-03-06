namespace IncuSmart.Infra.Persistences.Mappers
{
    public class IncubatorConfigInstanceMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<IncubatorConfigInstanceEntity, IncubatorConfigInstance>();
            config.NewConfig<IncubatorConfigInstance, IncubatorConfigInstanceEntity>();
            config.NewConfig<List<IncubatorConfigInstance>, List<IncubatorConfigInstanceEntity>>();
        }
    }

}
