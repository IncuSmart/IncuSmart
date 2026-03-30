namespace IncuSmart.Core.Usecases
{
    public class HatchingSeasonUseCase : IHatchingSeasonUseCase
    {
        private readonly IHatchingSeasonRepository  _seasonRepo;
        private readonly IIncubatorRepository       _incubatorRepository;
        private readonly ICustomerRepository        _customerRepository;
        private readonly IUnitOfWork                _unitOfWork;
        private readonly ILogger<HatchingSeasonUseCase> _logger;

        public HatchingSeasonUseCase(
            IHatchingSeasonRepository seasonRepo,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<HatchingSeasonUseCase> logger)
        {
            _seasonRepo          = seasonRepo;
            _incubatorRepository = incubatorRepository;
            _customerRepository  = customerRepository;
            _unitOfWork          = unitOfWork;
            _logger              = logger;
        }

        // ─── CREATE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<Guid?>> Create(CreateHatchingSeasonCommand command)
        {
            // Validate: incubator tồn tại
            var incubator = await _incubatorRepository.FindById(command.IncubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<Guid?>("404", "Không tìm thấy máy ấp", null);

            await _unitOfWork.BeginAsync();
            try
            {
                //Chua co ham GenerateSeasonCode
                //var seasonCode = CodeGenUtils.GenerateSeasonCode();

                var season = new HatchingSeason
                {
                    Id          = Guid.NewGuid(),
                    IncubatorId = command.IncubatorId,
                    TemplateId  = command.TemplateId,
                    SeasonCode = "seasonCode",
                    //SeasonCode  = seasonCode,
                    Name = command.Name,
                    EggType     = command.EggType,
                    StartDate   = command.StartDate,
                    TotalEggs   = command.TotalEggs,
                    Notes       = command.Notes,
                    SuccessCount = 0,
                    FailCount    = 0,
                    Status      = BaseStatus.ACTIVE,
                    CreatedAt   = DateTime.UtcNow,
                    CreatedBy   = "SYSTEM",
                };
                await _seasonRepo.Add(season);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Tạo mùa ấp thành công", season.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating hatching season");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        // ─── GET ALL ───────────────────────────────────────────────────────────────
        // CUSTOMER: chỉ thấy mùa ấp của máy mình
        // ADMIN/TECHNICIAN: lấy theo incubatorId (optional)
        public async Task<ResultModel<List<HatchingSeason>>> GetAll(
            Guid? incubatorId, Guid? currentUserId, string role)
        {
            Guid? resolvedCustomerId = null;

            if (role == "CUSTOMER" && currentUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                    return ResultModelUtils.FillResult<List<HatchingSeason>>("404", "Không tìm thấy thông tin khách hàng", new());
                resolvedCustomerId = customer.Id;
            }

            var list = await _seasonRepo.FindAll(incubatorId, resolvedCustomerId);
            return ResultModelUtils.FillResult<List<HatchingSeason>>("200", "Success", list);
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<bool>> Update(UpdateHatchingSeasonCommand command)
        {
            var season = await _seasonRepo.FindById(command.Id);
            if (season == null)
                return ResultModelUtils.FillResult<bool>("404", "Không tìm thấy mùa ấp", false);

            await _unitOfWork.BeginAsync();
            try
            {
                season.Name         = command.Name         ?? season.Name;
                season.EggType      = command.EggType      ?? season.EggType;
                season.EndDate      = command.EndDate      ?? season.EndDate;
                season.TotalEggs    = command.TotalEggs    ?? season.TotalEggs;
                season.SuccessCount = command.SuccessCount ?? season.SuccessCount;
                season.FailCount    = command.FailCount    ?? season.FailCount;
                season.Notes        = command.Notes        ?? season.Notes;
                if (command.Status != null)
                    season.Status = Enum.Parse<BaseStatus>(command.Status);
                season.UpdatedAt = DateTime.UtcNow;
                season.UpdatedBy = "SYSTEM";
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Cập nhật mùa ấp thành công", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating hatching season {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }
}
