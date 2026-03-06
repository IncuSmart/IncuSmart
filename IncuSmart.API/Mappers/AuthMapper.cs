namespace IncuSmart.API.Mappers
{
    public class AuthMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<LoginRequest, LoginCommand>();
            config.NewConfig<RegisterRequest, RegisterCommand>();
        }
    }
}
