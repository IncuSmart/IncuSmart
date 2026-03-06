using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Ports.Inbound
{
    public interface IIncubatorModelUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateIncubatorModelCommand command);
        Task<ResultModel<IncubatorModel?>> GetById(Guid id);
        Task<ResultModel<List<IncubatorModel>>> GetAll();
        Task<ResultModel<bool>> Update(UpdateIncubatorModelCommand command);
        Task<ResultModel<bool>> Delete(Guid id);
    }

}
