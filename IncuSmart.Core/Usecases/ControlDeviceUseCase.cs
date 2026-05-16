namespace IncuSmart.Core.Usecases
{
    public class ControlDeviceUseCase : IControlDeviceUseCase
    {
        private readonly IControlDeviceRepository _controlDeviceRepository;
        private readonly IMasterboardRepository _masterboardRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ControlDeviceUseCase> _logger;

        public ControlDeviceUseCase(
            IControlDeviceRepository controlDeviceRepository,
            IMasterboardRepository masterboardRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IConfigRepository configRepository,
            IUnitOfWork unitOfWork,
            ILogger<ControlDeviceUseCase> logger)
        {
            _controlDeviceRepository = controlDeviceRepository;
            _masterboardRepository = masterboardRepository;
            _incubatorRepository = incubatorRepository;
            _customerRepository = customerRepository;
            _configRepository = configRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateControlDeviceCommand command)
        {
            if (!string.IsNullOrWhiteSpace(command.HardwareCode))
            {
                var duplicatedByHardwareCode = await _controlDeviceRepository.FindByHardwareCode(command.HardwareCode.Trim());
                if (duplicatedByHardwareCode != null)
                {
                    return ResultModelUtils.FillResult<Guid?>("409", CommonConst.ControlDeviceHardwareCodeAlreadyExists, null);
                }
            }

            var masterboard = await _masterboardRepository.FindById(command.MasterboardId);
            if (masterboard == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.MasterboardNotFound, null);
            }

            var config = await _configRepository.FindById(command.ConfigId);
            if (config == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.ConfigNotFound, null);
            }

            if (command.PinNumber.HasValue)
            {
                var duplicatedByPin = await _controlDeviceRepository.FindByMasterboardIdAndPinNumber(command.MasterboardId, command.PinNumber.Value);
                if (duplicatedByPin != null)
                {
                    return ResultModelUtils.FillResult<Guid?>("409", CommonConst.ControlDevicePinNumberAlreadyUsed, null);
                }
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var controlDevice = new ControlDevice
                {
                    Id = Guid.NewGuid(),
                    MasterboardId = command.MasterboardId,
                    ControlBoardTypesId = command.ControlBoardTypesId,
                    ConfigId = command.ConfigId,
                    HardwareCode = string.IsNullOrWhiteSpace(command.HardwareCode) ? null : command.HardwareCode.Trim(),
                    PinNumber = command.PinNumber,
                    State = string.IsNullOrWhiteSpace(command.State) ? null : command.State.Trim(),
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CommonConst.SystemActor,
                };

                await _controlDeviceRepository.Add(controlDevice);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateControlDeviceSuccessfully, controlDevice.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating control device");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<List<ControlDevice>>> GetByIncubatorId(Guid incubatorId, Guid? currentUserId, string role)
        {
            var incubator = await _incubatorRepository.FindById(incubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<List<ControlDevice>>("404", CommonConst.IncubatorNotFound, new());
            }

            if (role == UserRole.CUSTOMER.ToString() && currentUserId.HasValue && currentUserId != Guid.Empty)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                {
                    return ResultModelUtils.FillResult<List<ControlDevice>>("404", CommonConst.CustomerNotFound, new());
                }

                if (incubator.CustomerId != customer.Id)
                {
                    return ResultModelUtils.FillResult<List<ControlDevice>>("403", CommonConst.AccessDenied, new());
                }
            }

            var masterboard = await _masterboardRepository.FindByIncubatorId(incubatorId);
            if (masterboard == null)
            {
                return ResultModelUtils.FillResult<List<ControlDevice>>("200", CommonConst.IncubatorHasNoMasterboard, new());
            }

            var list = await _controlDeviceRepository.FindByMasterboardId(masterboard.Id);
            return ResultModelUtils.FillResult<List<ControlDevice>>("200", CommonConst.Success, list);
        }
    }
}
