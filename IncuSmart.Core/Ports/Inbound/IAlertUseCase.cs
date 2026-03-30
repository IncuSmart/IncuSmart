using System.Security.Claims;
using IncuSmart.Core.Domains;


namespace IncuSmart.Core.Ports.Inbound;

public interface IAlertUseCase
{
    Task<ResultModel<Alert>> GetAlertById(Guid id, ClaimsPrincipal principal);
    Task<ResultModel<IEnumerable<Alert>>> GetAllAlerts(ClaimsPrincipal principal, Guid? incubatorId, string? severity, string? status, DateTime? from, DateTime? to);
    Task<ResultModel<bool>> ResolveAlert(Guid id, string? message, ClaimsPrincipal principal);
}
