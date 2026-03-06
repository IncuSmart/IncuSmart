using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Ports.Outbound
{
    public interface IConfigRepository
    {
        Task Add(Config config);
        Task<Config?> FindById(Guid id);
        Task<List<Config>> FindAll();
        Task<bool> ExistsInModelConfig(Guid configId);
    }
}
