using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Services
{
    public interface IRedisService
    {
        Task SetAsync(RedisDto dto);
        Task<string?> GetAsync(string key);
        Task DeleteAsync(string key);
    }

}
