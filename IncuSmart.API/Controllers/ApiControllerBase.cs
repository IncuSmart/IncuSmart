namespace IncuSmart.API.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected async Task<IActionResult> FromResultAndAudit<T>(
            BaseResponse<T> response,
            IAuditLogUseCase auditLogUseCase,
            Guid userId,
            AuditAction action,
            AuditEntityType entity,
            Guid? entityId = null)
        {
            if (response.StatusCode == "200" && userId != Guid.Empty)
            {
                entityId ??= TryExtractEntityId(response.Data);
                await auditLogUseCase.Create(new CreateAuditLogCommand
                {
                    UserId = userId,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId
                });
            }

            return FromResult(response);
        }

        protected IActionResult FromResult<T>(BaseResponse<T> response)
        {
            return GetStatusCode(response.StatusCode) switch
            {
                API.StatusCode.SUCCESS => Ok(response),
                API.StatusCode.NOT_FOUND => NotFound(response),
                API.StatusCode.CONFLICT => Conflict(response),
                API.StatusCode.FORBIDDEN => StatusCode(StatusCodes.Status403Forbidden, response),
                API.StatusCode.BAD_REQUEST => BadRequest(response),
                _ => BadRequest(response)
            };
        }

        protected API.StatusCode GetStatusCode(string? statusCode)
        {
            return statusCode switch
            {
                "200" => API.StatusCode.SUCCESS,
                "404" => API.StatusCode.NOT_FOUND,
                "409" => API.StatusCode.CONFLICT,
                "403" => API.StatusCode.FORBIDDEN,
                "400" => API.StatusCode.BAD_REQUEST,
                _ => API.StatusCode.BAD_REQUEST
            };
        }

        private static Guid? TryExtractEntityId<T>(T? data)
        {
            if (data is Guid guid && guid != Guid.Empty)
                return guid;

            return null;
        }
    }
}
