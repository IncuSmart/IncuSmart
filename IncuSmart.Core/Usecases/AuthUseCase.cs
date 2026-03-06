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

            if (user == null) return ResultModelUtils.FillResult<string?>("404", "Wrong username or password", null);

            bool isPasswordValid = PasswordUtil.VerifyPassword(command.Password, user.PasswordHash);

            return isPasswordValid 
                ? ResultModelUtils.FillResult<string?>("200", "Login successfully", JwtUtil.GenerateToken(user)) 
                : ResultModelUtils.FillResult<string?>("404", "Wrong username or password", null);
        }

        public async Task<ResultModel<string?>> Register(RegisterCommand command)
        {
            User? user = await _userRepository
                        .FindByUserNameAndDeletedAtIsNull(
                            command.Username
                    );

            if (user != null) return ResultModelUtils.FillResult<string?>("404", "Username is existed", null);

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
                    CreatedBy = "SYSTEM",
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.Add(newUser);
                await _unitOfWork.SaveChangesAsync();

                Customer newCustomer = new()
                {
                    Status = BaseStatus.ACTIVE,
                    UserId = userId,
                    CreatedBy = "SYSTEM",
                    CreatedAt = DateTime.UtcNow
                };

                await _customerRepository.Add(newCustomer);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<string?>("200", "Register successfully", null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return ResultModelUtils.FillResult<string?>("500", ex.Message, null); 
            }
        }
    }
}
