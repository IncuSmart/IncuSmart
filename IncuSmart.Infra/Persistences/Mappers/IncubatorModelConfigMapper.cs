namespace IncuSmart.Infra.Persistences.Mappers
{
    public class IncubatorModelConfigMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<IncubatorModelConfigEntity, IncubatorModelConfig>();
            config.NewConfig<IncubatorModelConfig, IncubatorModelConfigEntity>();
            config.NewConfig<List<IncubatorModelConfigEntity>, List<IncubatorModelConfig>>(); 
        }
    }
}
