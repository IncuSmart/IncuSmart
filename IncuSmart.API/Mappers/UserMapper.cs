using IncuSmart.Core.Domain;

namespace IncuSmart.API.Mappers
{
    public class UserMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CreateUserRequest, CreateUserCommand>();
            config.NewConfig<UpdateUserRequest, UpdateUserCommand>();
            config.NewConfig<UpdateProfileRequest, UpdateProfileCommand>();
            config.NewConfig<UpdateCustomerProfileRequest, UpdateCustomerProfileCommand>();
            config.NewConfig<ChangePasswordRequest, ChangePasswordCommand>();
            config.NewConfig<ResetPasswordRequest, ResetPasswordCommand>();
            config.NewConfig<User, UserResponse>()
                .Map(dest => dest.Role, src => src.Role.ToString())
                .Map(dest => dest.Status, src => src.Status.ToString());
        }
    }
}
