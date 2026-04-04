using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Outbound;
using IncuSmart.Infra.Persistences.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace IncuSmart.Infra.Persistences.Repositories;

public class ControlDeviceRepository : IControlDeviceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ControlDeviceRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(ControlDevice controlDevice)
    {
        await _dbContext.ControlDevices.AddAsync(controlDevice.Adapt<ControlDeviceEntity>());
    }

    public async Task<ControlDevice?> FindById(Guid id)
    {
        var entity = await _dbContext.ControlDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        return entity?.Adapt<ControlDevice>();
    }

    public async Task<IEnumerable<ControlDevice>> GetAll()
    {
        return (await _dbContext.ControlDevices
            .AsNoTracking()
            .Where(x => x.DeletedAt == null)
            .ToListAsync())
            .Adapt<List<ControlDevice>>();
    }

    public async Task<IEnumerable<ControlDevice>> GetByMasterboardId(Guid masterboardId)
    {
        var entities = await _dbContext.ControlDevices
            .AsNoTracking()
            .Where(x => x.MasterboardId == masterboardId && x.DeletedAt == null)
            .Include(c => c.Config)
            .Include(c => c.ControlBoardType)
            .ToListAsync();
        return entities.Adapt<List<ControlDevice>>();
    }
}
