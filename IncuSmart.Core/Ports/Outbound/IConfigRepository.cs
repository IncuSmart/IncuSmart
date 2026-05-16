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
        Task<List<Config>> List(string? type, string? status);
        Task<List<Config>> FindByIds(List<Guid> ids);
        Task<bool> ExistsByCode(string code);
        Task<bool> ExistsInModelConfig(Guid configId);
    }

}
