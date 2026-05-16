using IncuSmart.Core.Enums;

namespace IncuSmart.Core.Usecases
{
    public class AuthUseCase : IAuthUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AuthUseCase(IUserRepository userRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResultModel<string?>> Login(LoginCommand command)
        {
            User? user = await _userRepository
                        .FindByUserNameAndDeletedAtIsNull(
                            command.Username
                    );

            if (user == null) return ResultModelUtils.FillResult<string?>("404", CommonConst.WrongUsernameOrPassword, null);

            if (user.Status != BaseStatus.ACTIVE)
                return ResultModelUtils.FillResult<string?>("403", CommonConst.UserAccountInactive, null);

            bool isPasswordValid = PasswordUtil.VerifyPassword(command.Password, user.PasswordHash);

            return isPasswordValid
                ? ResultModelUtils.FillResult<string?>("200", CommonConst.LoginSuccessfully, JwtUtil.GenerateToken(user))
                : ResultModelUtils.FillResult<string?>("404", CommonConst.WrongUsernameOrPassword, null);
        }

        public async Task<ResultModel<string?>> Register(RegisterCommand command)
        {
            command.Username = command.Username.Trim();
            command.FullName = command.FullName.Trim();
            command.Phone = command.Phone.Trim();
            command.Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();

            User? user = await _userRepository
                        .FindByUserNameAndDeletedAtIsNull(
                            command.Username
                    );

            if (user != null) return ResultModelUtils.FillResult<string?>("404", CommonConst.UsernameIsExisted, null);

            if (!string.IsNullOrWhiteSpace(command.Email))
            {
                var existingEmail = await _userRepository.FindByEmailAndDeletedAtIsNull(command.Email);
                if (existingEmail != null)
                    return ResultModelUtils.FillResult<string?>("409", CommonConst.EmailAlreadyExists, null);
            }

            var existingPhone = await _userRepository.FindByPhoneAndDeletedAtIsNull(command.Phone);
            if (existingPhone != null)
                return ResultModelUtils.FillResult<string?>("409", CommonConst.PhoneAlreadyExists, null);

            await _unitOfWork.BeginAsync();
            try
            {
                Guid userId = Guid.NewGuid();

                User newUser = new()
                {
                    Id = userId,
                    Username = command.Username,
                    PasswordHash = PasswordUtil.HashPassword(command.Password),
                    FullName = command.FullName,
                    Email = command.Email,
                    Phone = command.Phone,
                    Status = BaseStatus.ACTIVE,
                    Role = UserRole.CUSTOMER,
                    CreatedBy = CommonConst.SystemActor,
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.Add(newUser);
                await _unitOfWork.SaveChangesAsync();

                Customer newCustomer = new()
                {
                    Id = Guid.NewGuid(),
                    Status = BaseStatus.ACTIVE,
                    UserId = userId,
                    CreatedBy = CommonConst.SystemActor,
                    CreatedAt = DateTime.UtcNow
                };

                await _customerRepository.Add(newCustomer);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<string?>("200", CommonConst.RegisterSuccessfully, null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return ResultModelUtils.FillResult<string?>("500", ex.Message, null);
            }
        }
    }
}
