namespace IncuSmart.Core.Ports.Inbound
{
    public interface IUserUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateUserCommand command);
        Task<ResultModel<User?>> GetById(Guid id);
        Task<ResultModel<PagedResult<User>>> List(string? role, string? status, int page, int pageSize);
        Task<ResultModel<bool>> Update(UpdateUserCommand command);
        Task<ResultModel<bool>> UpdateProfile(UpdateProfileCommand command);
        Task<ResultModel<bool>> ChangePassword(ChangePasswordCommand command);
        Task<ResultModel<bool>> ResetPassword(ResetPasswordCommand command);
    }
}
