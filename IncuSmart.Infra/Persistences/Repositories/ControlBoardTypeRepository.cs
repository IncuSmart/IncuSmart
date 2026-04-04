using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Outbound;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace IncuSmart.Infra.Persistences.Repositories;

public class ControlBoardTypeRepository : IControlBoardTypeRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ControlBoardTypeRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ControlBoardType?> FindById(Guid id)
    {
        var entity = await _dbContext.ControlBoardTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        return entity?.Adapt<ControlBoardType>();
    }
}
