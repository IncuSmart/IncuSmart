namespace IncuSmart.API.Mappers
{
    public class AlertMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<ResolveAlertRequest, ResolveAlertCommand>();
        }
    }
}
