namespace IncuSmart.Core.Usecases
{
    public class WarrantyUseCase : IWarrantyUseCase
    {
        private readonly IWarrantyRepository _warrantyRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WarrantyUseCase> _logger;

        public WarrantyUseCase(
            IWarrantyRepository warrantyRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<WarrantyUseCase> logger)
        {
            _warrantyRepository = warrantyRepository;
            _incubatorRepository = incubatorRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateWarrantyCommand command)
        {
            var incubator = await _incubatorRepository.FindById(command.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.IncubatorNotFound, null);
            }

            if (command.StartDate.HasValue && command.EndDate.HasValue && command.StartDate > command.EndDate)
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.WarrantyStartDateCannotBeLaterThanEndDate, null);
            }

            var existingWarranty = await _warrantyRepository.FindByIncubatorId(command.IncubatorId);
            if (existingWarranty != null)
            {
                return ResultModelUtils.FillResult<Guid?>("409", CommonConst.WarrantyAlreadyExistsForIncubator, null);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var warranty = new Warranty
                {
                    Id = Guid.NewGuid(),
                    IncubatorId = command.IncubatorId,
                    StartDate = command.StartDate,
                    EndDate = command.EndDate,
                    Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CommonConst.SystemActor,
                };
                await _warrantyRepository.Add(warranty);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateWarrantySuccessfully, warranty.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating warranty");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<bool>> Update(UpdateWarrantyCommand command)
        {
            var warranty = await _warrantyRepository.FindById(command.Id);
            if (warranty == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.WarrantyNotFound, false);

            if (command.StartDate.HasValue && command.EndDate.HasValue && command.StartDate > command.EndDate)
                return ResultModelUtils.FillResult<bool>("400", CommonConst.WarrantyStartDateCannotBeLaterThanEndDate, false);

            await _unitOfWork.BeginAsync();
            try
            {
                warranty.StartDate = command.StartDate ?? warranty.StartDate;
                warranty.EndDate = command.EndDate ?? warranty.EndDate;
                warranty.Notes = command.Notes ?? warranty.Notes;
                warranty.UpdatedAt = DateTime.UtcNow;
                warranty.UpdatedBy = CommonConst.SystemActor;
                await _warrantyRepository.Update(warranty);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateWarrantySuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating warranty {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<Warranty?>> GetByIncubatorId(Guid incubatorId, Guid? currentUserId, string role)
        {
            var incubator = await _incubatorRepository.FindById(incubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<Warranty?>("404", CommonConst.IncubatorNotFound, null);
            }

            if (role == UserRole.CUSTOMER.ToString() && currentUserId.HasValue && currentUserId != Guid.Empty)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null || incubator.CustomerId != customer.Id)
                {
                    return ResultModelUtils.FillResult<Warranty?>("403", CommonConst.AccessDenied, null);
                }
            }

            var warranty = await _warrantyRepository.FindByIncubatorId(incubatorId);
            return warranty == null
                ? ResultModelUtils.FillResult<Warranty?>("404", CommonConst.WarrantyNotFound, null)
                : ResultModelUtils.FillResult<Warranty?>("200", CommonConst.Success, warranty);
        }
    }
}
