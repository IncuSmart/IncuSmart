namespace IncuSmart.Core.Utils
{
    public class RedisDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public TimeSpan? Expired { get; set; }
    }
}
