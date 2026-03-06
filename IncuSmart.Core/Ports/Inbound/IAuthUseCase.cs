namespace IncuSmart.Core.Ports.Inbound
{
    public interface IAuthUseCase
    {
        Task<ResultModel<string?>> Login(LoginCommand command);

        Task<ResultModel<string?>> Register(RegisterCommand command);
    }
}
