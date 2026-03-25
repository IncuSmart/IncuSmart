using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Ports.Inbound
{
    public interface IIncubatorUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateIncubatorCommand command);
        Task<ResultModel<Incubator?>> GetById(Guid id);
        Task<ResultModel<List<Incubator>>> GetAll();
        Task<ResultModel<bool>> Update(UpdateIncubatorCommand command);
        Task<ResultModel<bool>> Delete(Guid id);
    }
}
