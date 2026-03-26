namespace IncuSmart.Core.Usecases
{
    public class WarrantyUseCase : IWarrantyUseCase
    {
        private readonly IWarrantyRepository   _warrantyRepository;
        private readonly IIncubatorRepository  _incubatorRepository;
        private readonly ICustomerRepository   _customerRepository;
        private readonly IUnitOfWork           _unitOfWork;
        private readonly ILogger<WarrantyUseCase> _logger;

        public WarrantyUseCase(
            IWarrantyRepository warrantyRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<WarrantyUseCase> logger)
        {
            _warrantyRepository  = warrantyRepository;
            _incubatorRepository = incubatorRepository;
            _customerRepository  = customerRepository;
            _unitOfWork          = unitOfWork;
            _logger              = logger;
        }

        // ─── CREATE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<Guid?>> Create(CreateWarrantyCommand command)
        {
            // Validate: incubator tồn tại
            var incubator = await _incubatorRepository.FindById(command.IncubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<Guid?>("404", "Không tìm thấy máy ấp", null);

            await _unitOfWork.BeginAsync();
            try
            {
                var warranty = new Warranty
                {
                    Id          = Guid.NewGuid(),
                    IncubatorId = command.IncubatorId,
                    StartDate   = command.StartDate,
                    EndDate     = command.EndDate,
                    Notes       = command.Notes,
                    Status      = BaseStatus.ACTIVE,
                    CreatedAt   = DateTime.UtcNow,
                    CreatedBy   = "SYSTEM",
                };
                await _warrantyRepository.Add(warranty);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Tạo bảo hành thành công", warranty.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating warranty");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        // ─── GET BY INCUBATOR ID ────────────────────────────────────────────────────
        // CUSTOMER: chỉ xem được bảo hành của máy mình
        public async Task<ResultModel<Warranty?>> GetByIncubatorId(
            Guid incubatorId, Guid? currentUserId, string role)
        {
            var incubator = await _incubatorRepository.FindById(incubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<Warranty?>("404", "Không tìm thấy máy ấp", null);

            if (role == "CUSTOMER" && currentUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null || incubator.CustomerId != customer.Id)
                    return ResultModelUtils.FillResult<Warranty?>("400",
                        "Bạn không có quyền xem bảo hành của máy này", null);
            }

            var warranty = await _warrantyRepository.FindByIncubatorId(incubatorId);
            return warranty == null
                ? ResultModelUtils.FillResult<Warranty?>("404", "Máy chưa có thông tin bảo hành", null)
                : ResultModelUtils.FillResult<Warranty?>("200", "Success", warranty);
        }
    }
}
