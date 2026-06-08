using IncuSmart.Core.Enums;
using IncuSmart.Core.Services;
using System.Text.Json;

namespace IncuSmart.Core.Usecases
{
    public class AuthUseCase : IAuthUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisService _redisService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthUseCase> _logger;

        private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);
        private const string SessionKeyPrefix = "reg:";

        public AuthUseCase(
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            IRedisService redisService,
            IEmailService emailService,
            ILogger<AuthUseCase> logger)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _redisService = redisService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ResultModel<string?>> Login(LoginCommand command)
        {
            User? user = await _userRepository.FindByUserNameAndDeletedAtIsNull(command.Username);

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
            command.Email = command.Email?.Trim();

            if (string.IsNullOrWhiteSpace(command.Email))
                return ResultModelUtils.FillResult<string?>("400", CommonConst.EmailRequired, null);

            var existingUsername = await _userRepository.FindByUserNameAndDeletedAtIsNull(command.Username);
            if (existingUsername != null)
                return ResultModelUtils.FillResult<string?>("409", CommonConst.UsernameIsExisted, null);

            var existingEmail = await _userRepository.FindByEmailAndDeletedAtIsNull(command.Email);
            if (existingEmail != null)
                return ResultModelUtils.FillResult<string?>("409", CommonConst.EmailAlreadyExists, null);

            var existingPhone = await _userRepository.FindByPhoneAndDeletedAtIsNull(command.Phone);
            if (existingPhone != null)
                return ResultModelUtils.FillResult<string?>("409", CommonConst.PhoneAlreadyExists, null);

            var sessionId = Guid.NewGuid().ToString("N");
            var otp = OtpUtil.Generate6DigitOtp();

            var sessionData = JsonSerializer.Serialize(new Dictionary<string, string?>
            {
                ["username"] = command.Username,
                ["passwordHash"] = PasswordUtil.HashPassword(command.Password),
                ["fullName"] = command.FullName,
                ["email"] = command.Email,
                ["phone"] = command.Phone,
                ["otp"] = otp
            });

            await _redisService.SetAsync(new RedisDto
            {
                Key = SessionKeyPrefix + sessionId,
                Value = sessionData,
                Expired = SessionTtl
            });

            _ = _emailService.SendEmailAsync(new EmailDto
            {
                To = command.Email,
                Subject = "[IncuSmart] Mã OTP xác thực tài khoản",
                Body = EmailTemplates.RegistrationOtp(command.FullName, otp)
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "Gửi email OTP đăng ký thất bại, SessionId={SessionId}", sessionId);
            });

            return ResultModelUtils.FillResult<string?>("200", CommonConst.OtpSentSuccessfully, sessionId);
        }

        public async Task<ResultModel<string?>> VerifyRegistration(VerifyRegistrationCommand command)
        {
            var key = SessionKeyPrefix + command.SessionId;
            var raw = await _redisService.GetAsync(key);

            if (raw == null)
                return ResultModelUtils.FillResult<string?>("404", CommonConst.SessionNotFoundOrExpired, null);

            Dictionary<string, string?>? session;
            try
            {
                session = JsonSerializer.Deserialize<Dictionary<string, string?>>(raw);
            }
            catch
            {
                return ResultModelUtils.FillResult<string?>("500", CommonConst.SessionInvalid, null);
            }

            if (session == null)
                return ResultModelUtils.FillResult<string?>("500", CommonConst.SessionInvalid, null);

            if (!session.TryGetValue("otp", out var storedOtp) || storedOtp != command.Otp)
                return ResultModelUtils.FillResult<string?>("400", CommonConst.InvalidOtp, null);

            await _unitOfWork.BeginAsync();
            try
            {
                Guid userId = Guid.NewGuid();

                User newUser = new()
                {
                    Id = userId,
                    Username = session["username"] ?? string.Empty,
                    PasswordHash = session["passwordHash"] ?? string.Empty,
                    FullName = session["fullName"] ?? string.Empty,
                    Email = session.GetValueOrDefault("email"),
                    Phone = session["phone"] ?? string.Empty,
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

                await _redisService.DeleteAsync(key);

                return ResultModelUtils.FillResult<string?>("200", CommonConst.RegisterSuccessfully, null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return ResultModelUtils.FillResult<string?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<string?>> ResendRegistrationOtp(string sessionId)
        {
            var oldKey = SessionKeyPrefix + sessionId;
            var raw = await _redisService.GetAsync(oldKey);

            if (raw == null)
                return ResultModelUtils.FillResult<string?>("404", CommonConst.SessionNotFoundOrExpired, null);

            Dictionary<string, string?>? session;
            try { session = JsonSerializer.Deserialize<Dictionary<string, string?>>(raw); }
            catch { return ResultModelUtils.FillResult<string?>("500", CommonConst.SessionInvalid, null); }

            if (session == null)
                return ResultModelUtils.FillResult<string?>("500", CommonConst.SessionInvalid, null);

            var email = session.GetValueOrDefault("email");
            var fullName = session.GetValueOrDefault("fullName") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
                return ResultModelUtils.FillResult<string?>("400", CommonConst.EmailRequired, null);

            var newOtp = OtpUtil.Generate6DigitOtp();
            var newSessionId = Guid.NewGuid().ToString("N");

            session["otp"] = newOtp;
            var newSessionData = JsonSerializer.Serialize(session);

            await _redisService.DeleteAsync(oldKey);
            await _redisService.SetAsync(new RedisDto
            {
                Key = SessionKeyPrefix + newSessionId,
                Value = newSessionData,
                Expired = SessionTtl
            });

            _ = _emailService.SendEmailAsync(new EmailDto
            {
                To = email,
                Subject = "[IncuSmart] Mã OTP xác thực tài khoản (gửi lại)",
                Body = EmailTemplates.RegistrationOtp(fullName, newOtp)
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "Gửi lại email OTP đăng ký thất bại, SessionId={SessionId}", newSessionId);
            });

            return ResultModelUtils.FillResult<string?>("200", CommonConst.OtpSentSuccessfully, newSessionId);
        }
    }
}
