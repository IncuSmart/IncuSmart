using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class ControlDeviceRepository : IControlDeviceRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public ControlDeviceRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

        public async Task Add(ControlDevice controlDevice) =>
            await _dbContext.ControlDevices.AddAsync(controlDevice.Adapt<ControlDeviceEntity>());

        public async Task<ControlDevice?> FindByHardwareCode(string hardwareCode)
        {
            var entity = await _dbContext.ControlDevices
                .FirstOrDefaultAsync(x => x.HardwareCode == hardwareCode && x.DeletedAt == null);
            return entity?.Adapt<ControlDevice>();
        }

        public async Task<ControlDevice?> FindByMasterboardIdAndPinNumber(Guid masterboardId, int pinNumber)
        {
            var entity = await _dbContext.ControlDevices
                .FirstOrDefaultAsync(x => x.MasterboardId == masterboardId && x.PinNumber == pinNumber && x.DeletedAt == null);
            return entity?.Adapt<ControlDevice>();
        }

        // Include Config info và ControlBoardType info theo spec response shape
        public async Task<List<ControlDevice>> FindByMasterboardId(Guid masterboardId)
        {
            return (await _dbContext.ControlDevices
                .Include(cd => cd.Config)
                .Include(cd => cd.ControlBoardType)
                .Where(x => x.MasterboardId == masterboardId && x.DeletedAt == null)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync())
                .Adapt<List<ControlDevice>>();
        }
    }
}
