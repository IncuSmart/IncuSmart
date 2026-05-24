namespace IncuSmart.Core.Usecases
{
    public class WarrantyUseCase : IWarrantyUseCase
    {
        private readonly IWarrantyRepository _warrantyRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IMaintenanceTicketRepository _ticketRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WarrantyUseCase> _logger;

        public WarrantyUseCase(
            IWarrantyRepository warrantyRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IMaintenanceTicketRepository ticketRepository,
            IUnitOfWork unitOfWork,
            ILogger<WarrantyUseCase> logger)
        {
            _warrantyRepository = warrantyRepository;
            _incubatorRepository = incubatorRepository;
            _customerRepository = customerRepository;
            _ticketRepository = ticketRepository;
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
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                // Điều kiện 1: bảo hành đã hết hạn (có EndDate và EndDate < hôm nay) hoặc bị vô hiệu
                bool isExpiredOrInactive = existingWarranty.Status != BaseStatus.ACTIVE
                    || (existingWarranty.EndDate.HasValue && existingWarranty.EndDate.Value < today);

                // Điều kiện 2: tất cả ticket bảo trì của bảo hành này đã đóng
                bool allTicketsClosed = false;
                if (!isExpiredOrInactive)
                {
                    var tickets = await _ticketRepository.List(command.IncubatorId, null, null, null);
                    var warrantyTickets = tickets.Where(t => t.WarrantyId == existingWarranty.Id).ToList();
                    allTicketsClosed = warrantyTickets.Count == 0 || warrantyTickets.All(t =>
                        t.Status == MaintenanceTicketStatus.CLOSED ||
                        t.Status == MaintenanceTicketStatus.RESOLVED ||
                        t.Status == MaintenanceTicketStatus.REJECTED ||
                        t.Status == MaintenanceTicketStatus.CANCELLED);
                }

                if (!isExpiredOrInactive && !allTicketsClosed)
                    return ResultModelUtils.FillResult<Guid?>("409", CommonConst.WarrantyAlreadyExistsForIncubator, null);

                // Gia hạn: cập nhật lại warranty cũ thay vì tạo mới (tránh unique constraint DB)
                await _unitOfWork.BeginAsync();
                try
                {
                    existingWarranty.StartDate = command.StartDate;
                    existingWarranty.EndDate = command.EndDate;
                    existingWarranty.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
                    existingWarranty.Status = BaseStatus.ACTIVE;
                    existingWarranty.UpdatedAt = DateTime.UtcNow;
                    existingWarranty.UpdatedBy = CommonConst.SystemActor;
                    await _warrantyRepository.Update(existingWarranty);
                    await _unitOfWork.CommitAsync();
                    return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateWarrantySuccessfully, existingWarranty.Id);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackAsync();
                    _logger.LogError(ex, "Error renewing warranty");
                    return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
                }
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
