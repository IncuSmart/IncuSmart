namespace IncuSmart.Core.Services.Impl
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;

        public RedisService(IOptions<RedisOptions> options)
        {
            var connection = ConnectionMultiplexer.Connect(options.Value.ConnectionString);
            _db = connection.GetDatabase();
        }

        public async Task SetAsync(RedisDto dto) =>
            await _db.StringSetAsync(dto.Key, dto.Value, dto.Expired, When.Always);

        public async Task<string?> GetAsync(string key) =>
            (string?)await _db.StringGetAsync(key);

        public async Task DeleteAsync(string key) =>
            await _db.KeyDeleteAsync(key);
    }

}
