using IncuSmart.Core.Commands;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Enums;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Ports.Outbound;
using IncuSmart.Core.Utils;
using Microsoft.Extensions.Logging;

namespace IncuSmart.Core.Usecases;

public class ControlDeviceUseCase : IControlDeviceUseCase
{
    private readonly IControlDeviceRepository _controlDeviceRepository;
    private readonly IMasterboardRepository _masterboardRepository;
    private readonly IControlBoardTypeRepository _controlBoardTypeRepository;
    private readonly IConfigRepository _configRepository;
    private readonly IIncubatorRepository _incubatorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ControlDeviceUseCase> _logger;

    public ControlDeviceUseCase(
        IControlDeviceRepository controlDeviceRepository,
        IMasterboardRepository masterboardRepository,
        IControlBoardTypeRepository controlBoardTypeRepository,
        IConfigRepository configRepository,
        IIncubatorRepository incubatorRepository,
        IUnitOfWork unitOfWork,
        ILogger<ControlDeviceUseCase> logger)
    {
        _controlDeviceRepository = controlDeviceRepository;
        _masterboardRepository = masterboardRepository;
        _controlBoardTypeRepository = controlBoardTypeRepository;
        _configRepository = configRepository;
        _incubatorRepository = incubatorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResultModel<Guid?>> Create(CreateControlDeviceCommand command)
    {
        if (await _masterboardRepository.FindById(command.MasterboardId) is null)
            return ResultModelUtils.FillResult<Guid?>("404", "Masterboard not found", null);

        if (command.ControlBoardTypesId.HasValue && await _controlBoardTypeRepository.FindById(command.ControlBoardTypesId.Value) is null)
            return ResultModelUtils.FillResult<Guid?>("404", "Control Board Type not found", null);

        if (await _configRepository.FindById(command.ConfigId) is null)
            return ResultModelUtils.FillResult<Guid?>("404", "Config not found", null);

        await _unitOfWork.BeginAsync();
        try
        {
            var controlDevice = new ControlDevice
            {
                Id = Guid.NewGuid(),
                MasterboardId = command.MasterboardId,
                ControlBoardTypesId = command.ControlBoardTypesId,
                ConfigId = command.ConfigId,
                HardwareCode = command.HardwareCode,
                PinNumber = command.PinNumber,
                State = command.State,
                Status = BaseStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM",
            };

            await _controlDeviceRepository.Add(controlDevice);
            await _unitOfWork.CommitAsync();

            return ResultModelUtils.FillResult<Guid?>("201", "Control device created successfully", controlDevice.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error creating control device");
            return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
        }
    }

    public async Task<ResultModel<IEnumerable<ControlDevice>>> GetByIncubatorId(Guid incubatorId)
    {
        // TODO: Add customer ownership check based on current user's role and ID.
        // var incubator = await _incubatorRepository.FindById(incubatorId);
        // if (incubator is null)
        //     return ResultModelUtils.FillResult<IEnumerable<ControlDevice>>("404", "Incubator not found", null);
        //
        // (If user is customer, check if incubator.CustomerId matches user's customerId)

        var masterboard = await _masterboardRepository.FindByIncubatorId(incubatorId);
        if (masterboard is null)
        {
            // This is not an error, an incubator might not have a masterboard yet. Return empty list.
            return ResultModelUtils.FillResult<IEnumerable<ControlDevice>>("200", "Success", new List<ControlDevice>());
        }

        var controlDevices = await _controlDeviceRepository.GetByMasterboardId(masterboard.Id);

        return ResultModelUtils.FillResult("200", "Success", controlDevices);
    }
}
