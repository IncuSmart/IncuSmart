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
        
        public static ResultModel<T> Success<T>(T? data, string? message = null)
        {
            return FillResult("200", message, data);
        }

        public static ResultModel<T> BadRequest<T>(string? message)
        {
            return FillResult<T>("400", message, default);
        }

        public static ResultModel<T> Unauthorized<T>(string? message)
        {
            return FillResult<T>("401", message, default);
        }

        public static ResultModel<T> Forbidden<T>(string? message)
        {
            return FillResult<T>("403", message, default);
        }

        public static ResultModel<T> NotFound<T>(string? message)
        {
            return FillResult<T>("404", message, default);
        }

        public static ResultModel<T> Conflict<T>(string? message)
        {
            return FillResult<T>("409", message, default);
        }
    }
}
