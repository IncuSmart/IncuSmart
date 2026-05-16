namespace IncuSmart.Core.Usecases
{
    public class SensorReadingUseCase : ISensorReadingUseCase
    {
        private readonly ISensorReadingRepository _sensorReadingRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<SensorReadingUseCase> _logger;

        public SensorReadingUseCase(
            ISensorReadingRepository sensorReadingRepository,
            ISensorRepository sensorRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            ILogger<SensorReadingUseCase> logger)
        {
            _sensorReadingRepository = sensorReadingRepository;
            _sensorRepository = sensorRepository;
            _incubatorRepository = incubatorRepository;
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public async Task<ResultModel<List<SensorReading>>> GetByFilters(
            Guid incubatorId,
            Guid? sensorId,
            Guid? configId,
            DateTime? from,
            DateTime? to,
            int limit,
            Guid? currentUserId,
            string role)
        {
            if (limit <= 0)
            {
                return ResultModelUtils.FillResult<List<SensorReading>>("400", CommonConst.LimitMustBeGreaterThanZero, new());
            }

            if (from.HasValue && to.HasValue && from > to)
            {
                return ResultModelUtils.FillResult<List<SensorReading>>("400", CommonConst.FromMustBeEarlierThanTo, new());
            }

            var incubator = await _incubatorRepository.FindById(incubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<List<SensorReading>>("404", CommonConst.IncubatorNotFound, new());
            }

            if (role == UserRole.CUSTOMER.ToString() && currentUserId.HasValue && currentUserId != Guid.Empty)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                {
                    return ResultModelUtils.FillResult<List<SensorReading>>("404", CommonConst.CustomerNotFound, new());
                }

                if (incubator.CustomerId != customer.Id)
                {
                    return ResultModelUtils.FillResult<List<SensorReading>>("403", CommonConst.AccessDenied, new());
                }
            }

            var sensorIds = await _sensorRepository.FindSensorIdsByIncubatorId(incubatorId);
            if (!sensorIds.Any())
            {
                return ResultModelUtils.FillResult<List<SensorReading>>("200", CommonConst.IncubatorHasNoSensors, new());
            }

            if (sensorId.HasValue && !sensorIds.Contains(sensorId.Value))
            {
                return ResultModelUtils.FillResult<List<SensorReading>>("400", CommonConst.SensorDoesNotBelongToIncubator, new());
            }

            var effectiveLimit = Math.Min(limit, 1000);
            var readings = await _sensorReadingRepository.FindByFilters(sensorIds, sensorId, configId, from, to, effectiveLimit);
            return ResultModelUtils.FillResult<List<SensorReading>>("200", CommonConst.Success, readings);
        }
    }
}
