namespace IncuSmart.Core
{
    public class ResultModel<T>
    {
        public string StatusCode = "200";
        public string? Message; 
        public T? Data;
    }
}
