using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Outbound;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace IncuSmart.Infra.Persistences.Repositories;

public class MasterboardRepository : IMasterboardRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MasterboardRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Masterboard?> FindById(Guid id)
    {
        var entity = await _dbContext.Masterboards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        return entity?.Adapt<Masterboard>();
    }

    public async Task<Masterboard?> FindByIncubatorId(Guid incubatorId)
    {
        var entity = await _dbContext.Masterboards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IncubatorId == incubatorId && x.DeletedAt == null);
        return entity?.Adapt<Masterboard>();
    }
}
