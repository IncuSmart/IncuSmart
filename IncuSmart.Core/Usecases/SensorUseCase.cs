namespace IncuSmart.Core.Usecases
{
    public class SensorUseCase : ISensorUseCase
    {
        private readonly ISensorRepository                 _sensorRepository;
        private readonly IMasterboardRepository            _masterboardRepository;
        private readonly IIncubatorRepository             _incubatorRepository;
        private readonly IIncubatorConfigInstanceRepository _configInstanceRepository;
        private readonly ICustomerRepository               _customerRepository;
        private readonly IUnitOfWork                       _unitOfWork;
        private readonly ILogger<SensorUseCase>            _logger;

        public SensorUseCase(
            ISensorRepository sensorRepository,
            IMasterboardRepository masterboardRepository,
            IIncubatorRepository incubatorRepository,
            IIncubatorConfigInstanceRepository configInstanceRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<SensorUseCase> logger)
        {
            _sensorRepository        = sensorRepository;
            _masterboardRepository   = masterboardRepository;
            _incubatorRepository     = incubatorRepository;
            _configInstanceRepository = configInstanceRepository;
            _customerRepository      = customerRepository;
            _unitOfWork              = unitOfWork;
            _logger                  = logger;
        }

        // ─── CREATE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<Guid?>> Create(CreateSensorCommand command)
        {
            // Validate: masterboard tồn tại
            var masterboard = await _masterboardRepository.FindById(command.MasterboardId);
            if (masterboard == null)
                return ResultModelUtils.FillResult<Guid?>("404", "Không tìm thấy masterboard", null);

            // Validate: config instance tồn tại và thuộc cùng incubator với masterboard
            var configInstance = await _configInstanceRepository.FindById(command.ConfigInstanceId);
            if (configInstance == null)
                return ResultModelUtils.FillResult<Guid?>("404", "Không tìm thấy config instance", null);

            if (configInstance.IncubatorId != masterboard.IncubatorId)
                return ResultModelUtils.FillResult<Guid?>("400",
                    "Config instance không thuộc cùng incubator với masterboard", null);

            await _unitOfWork.BeginAsync();
            try
            {
                var sensor = new Sensor
                {
                    Id               = Guid.NewGuid(),
                    MasterboardId    = command.MasterboardId,
                    ConfigInstanceId = command.ConfigInstanceId,
                    HardwareCode     = command.HardwareCode,
                    PinNumber        = command.PinNumber,
                    Status           = BaseStatus.ACTIVE,
                    CreatedAt        = DateTime.UtcNow,
                    CreatedBy        = "SYSTEM",
                };
                await _sensorRepository.Add(sensor);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Tạo sensor thành công", sensor.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating sensor");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        // ─── GET BY INCUBATOR ID ────────────────────────────────────────────────────
        // Response include Config info (name, unit, type) qua ConfigInstance
        public async Task<ResultModel<List<Sensor>>> GetByIncubatorId(
            Guid incubatorId, Guid? currentUserId, string role)
        {
            // Kiểm tra incubator tồn tại
            var incubator = await _incubatorRepository.FindById(incubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<List<Sensor>>("404", "Không tìm thấy máy ấp", new());

            // Role CUSTOMER: kiểm tra incubator thuộc customer này
            if (role == "CUSTOMER" && currentUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                    return ResultModelUtils.FillResult<List<Sensor>>("404", "Không tìm thấy thông tin khách hàng", new());

                if (incubator.CustomerId != customer.Id)
                    return ResultModelUtils.FillResult<List<Sensor>>("400",
                        "Bạn không có quyền xem sensor của máy này", new());
            }

            // Lấy masterboard của incubator
            var masterboard = await _masterboardRepository.FindByIncubatorId(incubatorId);
            if (masterboard == null)
                return ResultModelUtils.FillResult<List<Sensor>>("200", "Máy chưa có masterboard", new());

            var sensors = await _sensorRepository.FindByMasterboardId(masterboard.Id);
            return ResultModelUtils.FillResult<List<Sensor>>("200", "Success", sensors);
        }
    }
}
