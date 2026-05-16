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

        public async Task<GuestOrderInfo?> FindByOrderId(Guid orderId)
        {
            var entity = await _dbContext.GuestOrderInfos
                .FirstOrDefaultAsync(x => x.OrderId == orderId && x.DeletedAt == null);
            return entity?.Adapt<GuestOrderInfo>();
        }

        public async Task Update(GuestOrderInfo guestOrderInfo)
        {
            var entity = await _dbContext.GuestOrderInfos
                .FirstOrDefaultAsync(x => x.Id == guestOrderInfo.Id && x.DeletedAt == null);

            if (entity != null)
            {
                guestOrderInfo.Adapt(entity);
            }
        }
    }
}
