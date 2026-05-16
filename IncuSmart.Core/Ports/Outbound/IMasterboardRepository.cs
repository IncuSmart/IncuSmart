using System;
using System.Threading.Tasks;

namespace IncuSmart.Core.Ports.Outbound
{
    public interface IMasterboardRepository
    {
        Task<Masterboard?> FindById(Guid id);
        Task<Masterboard?> FindByIncubatorId(Guid incubatorId);
    }
}
