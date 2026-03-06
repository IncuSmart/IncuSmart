namespace IncuSmart.Core.Utils
{
    public class ResultModelUtils
    {
        public static ResultModel<T> FillResult<T>(string statusCode, string? message, T? data) {
            return new ResultModel<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = data
            };
        }
    }
}
