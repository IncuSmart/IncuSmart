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

    public AlertUseCase(
        IAlertRepository alertRepository,
        IIncubatorRepository incubatorRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository)
    {
        _alertRepository = alertRepository;
        _incubatorRepository = incubatorRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
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
}