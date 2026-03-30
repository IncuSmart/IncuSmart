using System.Security.Claims;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Enums;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Ports.Outbound;
using IncuSmart.Core.Utils;
using static IncuSmart.Core.Utils.ResultModelUtils;

namespace IncuSmart.Core.Usecases;

public class AlertUseCase : IAlertUseCase
{
    private readonly IAlertRepository _alertRepository;
    private readonly IIncubatorRepository _incubatorRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AlertUseCase(
        IAlertRepository alertRepository,
        IIncubatorRepository incubatorRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _alertRepository = alertRepository;
        _incubatorRepository = incubatorRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResultModel<Alert>> GetAlertById(Guid id, ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized<Alert>("Invalid user identifier");
        }

        var user = await _userRepository.FindById(userId);
        if (user is null)
        {
            return Unauthorized<Alert>("User not found");
        }

        var alert = await _alertRepository.FindByIdWithDetailsAsync(id);
        if (alert is null)
        {
            return NotFound<Alert>("Alert not found");
        }

        if (user.Role == UserRole.CUSTOMER)
        {
            var customer = await _customerRepository.FindById(user.Id);
            if (customer is null || alert.Incubator?.CustomerId != customer.Id)
            {
                return Forbidden<Alert>("You are not authorized to view this alert");
            }
        }
        else if (user.Role != UserRole.ADMIN && user.Role != UserRole.TECHNICIAN)
        {
            return Forbidden<Alert>("You are not authorized to view this alert");
        }

        return Success(alert);
    }

    public async Task<ResultModel<IEnumerable<Alert>>> GetAllAlerts(ClaimsPrincipal principal, Guid? incubatorId, string? severity, string? status, DateTime? from, DateTime? to)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized<IEnumerable<Alert>>("Invalid user identifier");

        var user = await _userRepository.FindById(userId);
        if (user is null)
            return Unauthorized<IEnumerable<Alert>>("User not found");

        List<Guid>? customerIncubatorIds = null;
        if (user.Role == UserRole.CUSTOMER)
        {
            var customer = await _customerRepository.FindByUserId(user.Id);
            if (customer is null)
                return Forbidden<IEnumerable<Alert>>("Customer not found for this user.");
            
            var incubators = await _incubatorRepository.FindByCustomerId(customer.Id);
            customerIncubatorIds = incubators.Select(i => i.Id).ToList();

            if (incubatorId.HasValue && !customerIncubatorIds.Contains(incubatorId.Value))
            {
                return Forbidden<IEnumerable<Alert>>("You are not authorized to view alerts for this incubator.");
            }
        }

        var alerts = await _alertRepository.GetAllAsync(
            user.Role == UserRole.CUSTOMER ? customerIncubatorIds : null,
            incubatorId, 
            severity, 
            status, 
            from, 
            to);

        return Success(alerts);
    }

    public async Task<ResultModel<bool>> ResolveAlert(Guid id, string? message, ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized<bool>("Invalid user identifier");

        var user = await _userRepository.FindById(userId);
        if (user is null)
            return Unauthorized<bool>("User not found");

        var alert = await _alertRepository.FindByIdWithDetailsAsync(id);
        if (alert is null)
            return NotFound<bool>("Alert not found");

        // Assuming OPEN status is represented by ACTIVE. This should be clarified.
        if (alert.Status != BaseStatus.ACTIVE)
            return BadRequest<bool>("Alert is already resolved or closed.");

        // The request specifies setting status to RESOLVED_MANUAL, but the enum is BaseStatus.
        // Setting to DEACTIVE as a placeholder. The Alert status model needs to be revisited.
        alert.Status = BaseStatus.DEACTIVE;
        alert.ResolvedBy = "MANUAL";
        alert.Message = message; // Overwriting message as per interpretation of "Ghi chú xử lý"

        await _alertRepository.UpdateAsync(alert);
        await _unitOfWork.SaveChangesAsync();

        return Success(true);
    }
}