namespace IncuSmart.API.Responses

{
    public class BaseResponse<T>
    {
        public string StatusCode { get; set; } = "200";
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
