namespace IncuSmart.Core.Usecases
{
    public class HatchingSeasonUseCase : IHatchingSeasonUseCase
    {
        private readonly IHatchingSeasonRepository _seasonRepo;
        private readonly IHatchingSeasonTemplateRepository _templateRepository;
        private readonly IHatchingBatchRepository _batchRepository;
        private readonly IHatchingBatchConfigRepository _batchConfigRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HatchingSeasonUseCase> _logger;

        public HatchingSeasonUseCase(
            IHatchingSeasonRepository seasonRepo,
            IHatchingSeasonTemplateRepository templateRepository,
            IHatchingBatchRepository batchRepository,
            IHatchingBatchConfigRepository batchConfigRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<HatchingSeasonUseCase> logger)
        {
            _seasonRepo = seasonRepo;
            _templateRepository = templateRepository;
            _batchRepository = batchRepository;
            _batchConfigRepository = batchConfigRepository;
            _incubatorRepository = incubatorRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateHatchingSeasonCommand command, Guid? currentUserId, string role)
        {
            var incubator = await _incubatorRepository.FindById(command.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.IncubatorNotFound, null);
            }

            if (incubator.Status is not IncubatorStatus.ACTIVE and not IncubatorStatus.RESERVED)
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.IncubatorNotAvailableForOperation, null);
            }

            if (!await CanAccessIncubator(incubator, currentUserId, role))
            {
                return ResultModelUtils.FillResult<Guid?>("403", CommonConst.AccessDenied, null);
            }

            if (!command.TemplateId.HasValue)
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.TemplateIdRequired, null);
            }

            var template = await _templateRepository.FindById(command.TemplateId.Value);
            if (template == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.HatchingSeasonTemplateNotFound, null);
            }

            if (!await CanUseTemplate(template, currentUserId, role))
            {
                return ResultModelUtils.FillResult<Guid?>("403", CommonConst.AccessDenied, null);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var seasonCode = CodeGenUtils.GenerateSeasonCode();
                var season = new HatchingSeason
                {
                    Id = Guid.NewGuid(),
                    IncubatorId = command.IncubatorId,
                    TemplateId = template.Id,
                    SeasonCode = seasonCode,
                    Name = template.Name,
                    EggType = template.EggType,
                    StartDate = command.StartDate,
                    TotalEggs = command.TotalEggs,
                    Notes = command.Notes,
                    SuccessCount = 0,
                    FailCount = 0,
                    Status = HatchingSeasonStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                };
                await _seasonRepo.Add(season);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateHatchingSeasonSuccessfully, season.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating hatching season");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<HatchingSeasonDetailResponse?>> GetById(Guid id, Guid? currentUserId, string role)
        {
            var season = await _seasonRepo.FindById(id);
            if (season == null)
            {
                return ResultModelUtils.FillResult<HatchingSeasonDetailResponse?>("404", CommonConst.HatchingSeasonNotFound, null);
            }

            var incubator = await _incubatorRepository.FindById(season.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<HatchingSeasonDetailResponse?>("404", CommonConst.IncubatorNotFound, null);
            }

            if (!await CanAccessIncubator(incubator, currentUserId, role))
            {
                return ResultModelUtils.FillResult<HatchingSeasonDetailResponse?>("403", CommonConst.AccessDenied, null);
            }

            HatchingSeasonTemplate? template = null;
            if (season.TemplateId.HasValue)
            {
                template = await _templateRepository.FindById(season.TemplateId.Value);
            }

            var batches = await _batchRepository.FindBySeasonId(season.Id);
            var batchDetails = new List<HatchingBatchDetailResponse>();
            foreach (var batch in batches)
            {
                var configs = await _batchConfigRepository.FindByBatchId(batch.Id);
                batchDetails.Add(new HatchingBatchDetailResponse
                {
                    Batch = batch,
                    Configs = configs
                });
            }

            return ResultModelUtils.FillResult<HatchingSeasonDetailResponse?>("200", CommonConst.Success, new HatchingSeasonDetailResponse
            {
                Season = season,
                Template = template,
                Batches = batchDetails
            });
        }

        public async Task<ResultModel<PagedResult<HatchingSeason>>> List(Guid? incubatorId, Guid? filterCustomerId, string? status, Guid? currentUserId, string role, int page, int pageSize)
        {
            Guid? resolvedCustomerId = filterCustomerId;

            if (role == UserRole.CUSTOMER.ToString() && currentUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                {
                    return ResultModelUtils.FillResult<PagedResult<HatchingSeason>>("404", CommonConst.CustomerInformationNotFound, null);
                }
                resolvedCustomerId = customer.Id;
            }

            var list = await _seasonRepo.List(incubatorId, resolvedCustomerId, status);
            return ResultModelUtils.FillResult<PagedResult<HatchingSeason>>("200", CommonConst.Success, PagingUtils.ToPagedResult(list, page, pageSize));
        }

        public async Task<ResultModel<bool>> Update(UpdateHatchingSeasonCommand command, Guid? currentUserId, string role)
        {
            var season = await _seasonRepo.FindById(command.Id);
            if (season == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.HatchingSeasonNotFound, false);
            }

            var incubator = await _incubatorRepository.FindById(season.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorNotFound, false);
            }

            if (!await CanAccessIncubator(incubator, currentUserId, role))
            {
                return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                season.EndDate = command.EndDate ?? season.EndDate;
                season.TotalEggs = command.TotalEggs ?? season.TotalEggs;
                season.SuccessCount = command.SuccessCount ?? season.SuccessCount;
                season.FailCount = command.FailCount ?? season.FailCount;
                season.Notes = command.Notes ?? season.Notes;
                season.UpdatedAt = DateTime.UtcNow;
                season.UpdatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor;

                var totalEggs = season.TotalEggs;
                var successCount = season.SuccessCount;
                var failCount = season.FailCount;
                if (successCount + failCount > totalEggs)
                {
                    return ResultModelUtils.FillResult<bool>("400", CommonConst.EggCountExceedsTotal, false);
                }

                await _seasonRepo.Update(season);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateHatchingSeasonSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating hatching season {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> UpdateStatus(UpdateHatchingSeasonStatusCommand command, Guid? currentUserId, string role)
        {
            var season = await _seasonRepo.FindById(command.Id);
            if (season == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.HatchingSeasonNotFound, false);
            }

            var incubator = await _incubatorRepository.FindById(season.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorNotFound, false);
            }

            if (!await CanAccessIncubator(incubator, currentUserId, role))
            {
                return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);
            }

            if (!Enum.TryParse<HatchingSeasonStatus>(command.Status, true, out var nextStatus))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.InvalidSeasonStatus, false);
            }

            if (!CanTransition(season.Status, nextStatus))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.InvalidSeasonStatusTransition, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                season.Status = nextStatus;
                if (nextStatus == HatchingSeasonStatus.COMPLETED
                    || nextStatus == HatchingSeasonStatus.FAILED
                    || nextStatus == HatchingSeasonStatus.CANCELLED)
                {
                    season.EndDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
                }

                season.UpdatedAt = DateTime.UtcNow;
                season.UpdatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor;
                await _seasonRepo.Update(season);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateHatchingSeasonStatusSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating season status {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        private static bool CanTransition(HatchingSeasonStatus currentStatus, HatchingSeasonStatus nextStatus)
        {
            if (currentStatus == nextStatus)
            {
                return true;
            }

            return currentStatus switch
            {
                HatchingSeasonStatus.ACTIVE => nextStatus is HatchingSeasonStatus.COMPLETED
                    or HatchingSeasonStatus.FAILED
                    or HatchingSeasonStatus.CANCELLED,
                _ => false
            };
        }

        private async Task<bool> CanAccessIncubator(Incubator incubator, Guid? currentUserId, string role)
        {
            if (role == UserRole.ADMIN.ToString() || role == UserRole.TECHNICIAN.ToString())
            {
                return true;
            }

            if (role != UserRole.CUSTOMER.ToString() || !currentUserId.HasValue)
            {
                return false;
            }

            var customer = await _customerRepository.FindByUserId(currentUserId.Value);
            return customer != null && incubator.CustomerId == customer.Id;
        }

        private async Task<bool> CanUseTemplate(HatchingSeasonTemplate template, Guid? currentUserId, string role)
        {
            if (role == UserRole.ADMIN.ToString() || role == UserRole.TECHNICIAN.ToString())
            {
                return true;
            }

            if (role != UserRole.CUSTOMER.ToString() || !currentUserId.HasValue)
            {
                return false;
            }

            var customer = await _customerRepository.FindByUserId(currentUserId.Value);
            if (customer == null)
            {
                return false;
            }

            return template.CreatedByType == UserRole.TECHNICIAN.ToString()
                || template.CustomerId == customer.Id;
        }
    }
}
