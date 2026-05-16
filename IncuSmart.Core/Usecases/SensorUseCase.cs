namespace IncuSmart.Core.Usecases
{
    public class SensorUseCase : ISensorUseCase
    {
        private readonly ISensorRepository _sensorRepository;
        private readonly IMasterboardRepository _masterboardRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly IIncubatorConfigInstanceRepository _configInstanceRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SensorUseCase> _logger;

        public SensorUseCase(
            ISensorRepository sensorRepository,
            IMasterboardRepository masterboardRepository,
            IIncubatorRepository incubatorRepository,
            IIncubatorConfigInstanceRepository configInstanceRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<SensorUseCase> logger)
        {
            _sensorRepository = sensorRepository;
            _masterboardRepository = masterboardRepository;
            _incubatorRepository = incubatorRepository;
            _configInstanceRepository = configInstanceRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateSensorCommand command)
        {
            if (!string.IsNullOrWhiteSpace(command.HardwareCode))
            {
                var duplicatedByHardwareCode = await _sensorRepository.FindByHardwareCode(command.HardwareCode.Trim());
                if (duplicatedByHardwareCode != null)
                {
                    return ResultModelUtils.FillResult<Guid?>("409", CommonConst.SensorHardwareCodeAlreadyExists, null);
                }
            }

            var masterboard = await _masterboardRepository.FindById(command.MasterboardId);
            if (masterboard == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.MasterboardNotFound, null);
            }

            var configInstance = await _configInstanceRepository.FindById(command.ConfigInstanceId);
            if (configInstance == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.ConfigInstanceNotFound, null);
            }

            if (configInstance.IncubatorId != masterboard.IncubatorId)
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.ConfigInstanceSameIncubatorRequired, null);
            }

            if (command.PinNumber.HasValue)
            {
                var duplicatedByPin = await _sensorRepository.FindByMasterboardIdAndPinNumber(command.MasterboardId, command.PinNumber.Value);
                if (duplicatedByPin != null)
                {
                    return ResultModelUtils.FillResult<Guid?>("409", CommonConst.SensorPinNumberAlreadyUsed, null);
                }
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var sensor = new Sensor
                {
                    Id = Guid.NewGuid(),
                    MasterboardId = command.MasterboardId,
                    ConfigInstanceId = command.ConfigInstanceId,
                    HardwareCode = string.IsNullOrWhiteSpace(command.HardwareCode) ? null : command.HardwareCode.Trim(),
                    PinNumber = command.PinNumber,
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CommonConst.SystemActor,
                };

                await _sensorRepository.Add(sensor);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateSensorSuccessfully, sensor.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating sensor");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<List<Sensor>>> GetByIncubatorId(Guid incubatorId, Guid? currentUserId, string role)
        {
            var incubator = await _incubatorRepository.FindById(incubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<List<Sensor>>("404", CommonConst.IncubatorNotFound, new());
            }

            if (role == UserRole.CUSTOMER.ToString() && currentUserId.HasValue && currentUserId != Guid.Empty)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                {
                    return ResultModelUtils.FillResult<List<Sensor>>("404", CommonConst.CustomerNotFound, new());
                }

                if (incubator.CustomerId != customer.Id)
                {
                    return ResultModelUtils.FillResult<List<Sensor>>("403", CommonConst.AccessDenied, new());
                }
            }

            var masterboard = await _masterboardRepository.FindByIncubatorId(incubatorId);
            if (masterboard == null)
            {
                return ResultModelUtils.FillResult<List<Sensor>>("200", CommonConst.IncubatorHasNoMasterboard, new());
            }

            var sensors = await _sensorRepository.FindByMasterboardId(masterboard.Id);
            return ResultModelUtils.FillResult<List<Sensor>>("200", CommonConst.Success, sensors);
        }
    }
}
