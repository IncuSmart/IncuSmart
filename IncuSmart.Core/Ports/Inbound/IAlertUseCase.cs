using System.Security.Claims;
using IncuSmart.Core.Domains;


namespace IncuSmart.Core.Ports.Inbound;

public interface IAlertUseCase
{
    Task<ResultModel<Alert>> GetAlertById(Guid id, ClaimsPrincipal principal);
}
