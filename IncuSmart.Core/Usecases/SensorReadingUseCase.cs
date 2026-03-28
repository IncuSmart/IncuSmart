namespace IncuSmart.Core.Usecases
{
    public class SensorReadingUseCase : ISensorReadingUseCase
    {
        private readonly ISensorReadingRepository _sensorReadingRepository;
        private readonly ISensorRepository        _sensorRepository;
        private readonly IIncubatorRepository     _incubatorRepository;
        private readonly ICustomerRepository      _customerRepository;
        private readonly ILogger<SensorReadingUseCase> _logger;

        public SensorReadingUseCase(
            ISensorReadingRepository sensorReadingRepository,
            ISensorRepository sensorRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            ILogger<SensorReadingUseCase> logger)
        {
            _sensorReadingRepository = sensorReadingRepository;
            _sensorRepository        = sensorRepository;
            _incubatorRepository     = incubatorRepository;
            _customerRepository      = customerRepository;
            _logger                  = logger;
        }

        // ─── GET BY FILTERS ────────────────────────────────────────────────────────
        // Lọc: sensorId?, configId?, from?, to?, limit (mặc định 100)
        // Trả về danh sách sắp xếp theo recorded_at DESC
        public async Task<ResultModel<List<SensorReading>>> GetByFilters(
            Guid      incubatorId,
            Guid?     sensorId,
            Guid?     configId,
            DateTime? from,
            DateTime? to,
            int       limit,
            Guid?     currentUserId,
            string    role)
        {
            // Kiểm tra incubator tồn tại
            var incubator = await _incubatorRepository.FindById(incubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<List<SensorReading>>("404", "Không tìm thấy máy ấp", new());

            // Role CUSTOMER: kiểm tra incubator thuộc customer này
            if (role == "CUSTOMER" && currentUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                    return ResultModelUtils.FillResult<List<SensorReading>>("404", "Không tìm thấy thông tin khách hàng", new());

                if (incubator.CustomerId != customer.Id)
                    return ResultModelUtils.FillResult<List<SensorReading>>("400",
                        "Bạn không có quyền xem dữ liệu vận hành của máy này", new());
            }

            // Lấy tất cả sensorIds thuộc incubator này (để filter)
            var sensorIds = await _sensorRepository.FindSensorIdsByIncubatorId(incubatorId);
            if (!sensorIds.Any())
                return ResultModelUtils.FillResult<List<SensorReading>>("200", "Máy chưa có sensor nào", new());

            // Cap limit tối đa 1000 để tránh quá tải time-series data
            var effectiveLimit = Math.Min(limit, 1000);

            var readings = await _sensorReadingRepository.FindByFilters(
                sensorIds, sensorId, configId, from, to, effectiveLimit);

            return ResultModelUtils.FillResult<List<SensorReading>>("200", "Success", readings);
        }
    }
}
