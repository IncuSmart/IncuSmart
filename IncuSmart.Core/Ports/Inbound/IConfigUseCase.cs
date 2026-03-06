using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Ports.Inbound
{
    public interface IConfigUseCase
    {
        Task<ResultModel<Guid?>> Create(CreateConfigCommand command);
        Task<ResultModel<Config?>> GetById(Guid id);
        Task<ResultModel<List<Config>>> GetAll();
        Task<ResultModel<bool>> Update(UpdateConfigCommand command);
        Task<ResultModel<bool>> Delete(Guid id);
    }

}
