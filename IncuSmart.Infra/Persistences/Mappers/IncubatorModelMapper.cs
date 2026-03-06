using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Mappers
{
    public class IncubatorModelMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<IncubatorModelEntity, IncubatorModel>();
            config.NewConfig<IncubatorModel, IncubatorModelEntity>();
        }
    }

}
