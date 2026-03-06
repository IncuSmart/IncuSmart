using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Ports.Outbound
{
    public interface IGuestOrderInfoRepository
    {
        Task Add(GuestOrderInfo guestOrderInfo);
    }
}
