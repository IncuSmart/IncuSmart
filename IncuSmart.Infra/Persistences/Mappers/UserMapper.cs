namespace IncuSmart.Infra.Persistences.Mapper
{
    public class UserMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<UserEntity, User>();
            config.NewConfig<User, UserEntity>();
        }
    }
}
