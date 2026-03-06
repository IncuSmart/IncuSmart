using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class GuestOrderInfoRepository : IGuestOrderInfoRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public GuestOrderInfoRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(GuestOrderInfo guestOrderInfo)
        {
            GuestOrderInfoEntity entity = guestOrderInfo.Adapt<GuestOrderInfoEntity>();
            await _dbContext.AddAsync(entity);
        }
    }
}
