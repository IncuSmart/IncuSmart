namespace IncuSmart.API.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult FromResult<T>(BaseResponse<T> response)
        {
            return GetStatusCode(response.StatusCode) switch
            {
                API.StatusCode.SUCCESS => Ok(response),
                API.StatusCode.NOT_FOUND => NotFound(response),
                API.StatusCode.CONFLICT => Conflict(response),
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
                "400" => API.StatusCode.BAD_REQUEST,
                _ => API.StatusCode.BAD_REQUEST
            };
        }
    }
}
