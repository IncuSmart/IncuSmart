namespace IncuSmart.Core.Usecases
{
    public class UserUseCase : IUserUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserUseCase> _logger;

        public UserUseCase(
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<UserUseCase> logger)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateUserCommand command)
        {
            command.Username = command.Username.Trim();
            command.FullName = command.FullName.Trim();
            command.Phone = command.Phone.Trim();
            command.Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();

            var existing = await _userRepository.FindByUserNameAndDeletedAtIsNull(command.Username);
            if (existing != null)
                return ResultModelUtils.FillResult<Guid?>("409", CommonConst.UsernameAlreadyExists, null);

            if (!string.IsNullOrWhiteSpace(command.Email))
            {
                var existingEmail = await _userRepository.FindByEmailAndDeletedAtIsNull(command.Email);
                if (existingEmail != null)
                    return ResultModelUtils.FillResult<Guid?>("409", CommonConst.EmailAlreadyExists, null);
            }

            var existingPhone = await _userRepository.FindByPhoneAndDeletedAtIsNull(command.Phone);
            if (existingPhone != null)
                return ResultModelUtils.FillResult<Guid?>("409", CommonConst.PhoneAlreadyExists, null);

            if (!Enum.TryParse<UserRole>(command.Role, out var role))
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.InvalidRole, null);

            await _unitOfWork.BeginAsync();
            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = command.Username,
                    PasswordHash = PasswordUtil.HashPassword(command.Password),
                    FullName = command.FullName,
                    Email = command.Email,
                    Phone = command.Phone,
                    Role = role,
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CommonConst.SystemActor
                };

                await _userRepository.Add(user);

                if (role == UserRole.CUSTOMER)
                {
                    await _customerRepository.Add(new Customer
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Status = BaseStatus.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = CommonConst.SystemActor
                    });
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateSuccessfully, user.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating user");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<User?>> GetById(Guid id)
        {
            var user = await _userRepository.FindById(id);
            return user == null
                ? ResultModelUtils.FillResult<User?>("404", CommonConst.UserNotFound, null)
                : ResultModelUtils.FillResult<User?>("200", CommonConst.Success, user);
        }

        public async Task<ResultModel<PagedResult<User>>> List(string? role, string? status, int page, int pageSize)
        {
            var list = await _userRepository.List(role, status);
            return ResultModelUtils.FillResult<PagedResult<User>>("200", CommonConst.Success, PagingUtils.ToPagedResult(list, page, pageSize));
        }

        public async Task<ResultModel<bool>> Update(UpdateUserCommand command)
        {
            var user = await _userRepository.FindById(command.Id);
            if (user == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.UserNotFound, false);

            var nextEmail = command.Email != null
                ? (string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim())
                : user.Email;
            var nextPhone = command.Phone != null
                ? command.Phone.Trim()
                : user.Phone;

            if (!string.IsNullOrWhiteSpace(nextEmail))
            {
                var existingEmail = await _userRepository.FindByEmailAndDeletedAtIsNull(nextEmail);
                if (existingEmail != null && existingEmail.Id != user.Id)
                    return ResultModelUtils.FillResult<bool>("409", CommonConst.EmailAlreadyExists, false);
            }

            var existingPhone = await _userRepository.FindByPhoneAndDeletedAtIsNull(nextPhone);
            if (existingPhone != null && existingPhone.Id != user.Id)
                return ResultModelUtils.FillResult<bool>("409", CommonConst.PhoneAlreadyExists, false);

            if (command.Role != null && !Enum.TryParse<UserRole>(command.Role, out _))
                return ResultModelUtils.FillResult<bool>("400", CommonConst.InvalidRole, false);

            if (command.Status != null && !Enum.TryParse<BaseStatus>(command.Status, out _))
                return ResultModelUtils.FillResult<bool>("400", CommonConst.InvalidStatus, false);

            var targetRole = command.Role != null
                ? Enum.Parse<UserRole>(command.Role)
                : user.Role;
            var targetStatus = command.Status != null
                ? Enum.Parse<BaseStatus>(command.Status)
                : user.Status;
            var customer = await _customerRepository.FindByUserId(user.Id);

            await _unitOfWork.BeginAsync();
            try
            {
                user.FullName = command.FullName?.Trim() ?? user.FullName;
                user.Email = nextEmail;
                user.Phone = nextPhone;
                user.Role = targetRole;
                user.Status = targetStatus;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = CommonConst.SystemActor;

                await _userRepository.Update(user);

                if (targetRole == UserRole.CUSTOMER)
                {
                    if (customer == null)
                    {
                        await _customerRepository.Add(new Customer
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            Status = targetStatus,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = CommonConst.SystemActor
                        });
                    }
                    else
                    {
                        customer.Status = targetStatus;
                        customer.UpdatedAt = DateTime.UtcNow;
                        customer.UpdatedBy = CommonConst.SystemActor;
                        await _customerRepository.Update(customer);
                    }
                }
                else if (customer != null)
                {
                    customer.Status = BaseStatus.DEACTIVE;
                    customer.UpdatedAt = DateTime.UtcNow;
                    customer.UpdatedBy = CommonConst.SystemActor;
                    await _customerRepository.Update(customer);
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating user {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> UpdateProfile(UpdateProfileCommand command)
        {
            var user = await _userRepository.FindById(command.Id);
            if (user == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.UserNotFound, false);

            var nextEmail = command.Email != null
                ? (string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim())
                : user.Email;
            var nextPhone = command.Phone != null
                ? command.Phone.Trim()
                : user.Phone;

            if (!string.IsNullOrWhiteSpace(nextEmail))
            {
                var existingEmail = await _userRepository.FindByEmailAndDeletedAtIsNull(nextEmail);
                if (existingEmail != null && existingEmail.Id != user.Id)
                    return ResultModelUtils.FillResult<bool>("409", CommonConst.EmailAlreadyExists, false);
            }

            var existingPhone = await _userRepository.FindByPhoneAndDeletedAtIsNull(nextPhone);
            if (existingPhone != null && existingPhone.Id != user.Id)
                return ResultModelUtils.FillResult<bool>("409", CommonConst.PhoneAlreadyExists, false);

            await _unitOfWork.BeginAsync();
            try
            {
                user.FullName = command.FullName?.Trim() ?? user.FullName;
                user.Email = nextEmail;
                user.Phone = nextPhone;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = CommonConst.SystemActor;

                await _userRepository.Update(user);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating profile {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> ChangePassword(ChangePasswordCommand command)
        {
            var user = await _userRepository.FindById(command.UserId);
            if (user == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.UserNotFound, false);
            }

            if (!PasswordUtil.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.CurrentPasswordIncorrect, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                user.PasswordHash = PasswordUtil.HashPassword(command.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = command.UserId.ToString();

                await _userRepository.Update(user);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.ChangePasswordSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error changing password for user {UserId}", command.UserId);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> ResetPassword(ResetPasswordCommand command)
        {
            var user = await _userRepository.FindById(command.UserId);
            if (user == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.UserNotFound, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                user.PasswordHash = PasswordUtil.HashPassword(command.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = CommonConst.SystemActor;

                await _userRepository.Update(user);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.ResetPasswordSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error resetting password for user {UserId}", command.UserId);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }
}
