using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Mappers
{
    public class MaintenanceTicketMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<MaintenanceTicketDetail, MaintenanceTicketDetailResponse>();
        }

    }
}
