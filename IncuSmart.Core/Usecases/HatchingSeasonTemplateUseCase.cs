namespace IncuSmart.Core.Usecases
{
    public class HatchingSeasonTemplateUseCase : IHatchingSeasonTemplateUseCase
    {
        private readonly IHatchingSeasonTemplateRepository _templateRepo;
        private readonly IHatchingSeasonTemplateBatchRepository _batchRepo;
        private readonly IHatchingSeasonTemplateBatchConfigRepository _batchConfigRepo;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HatchingSeasonTemplateUseCase> _logger;

        public HatchingSeasonTemplateUseCase(
            IHatchingSeasonTemplateRepository templateRepo,
            IHatchingSeasonTemplateBatchRepository batchRepo,
            IHatchingSeasonTemplateBatchConfigRepository batchConfigRepo,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<HatchingSeasonTemplateUseCase> logger)
        {
            _templateRepo = templateRepo;
            _batchRepo = batchRepo;
            _batchConfigRepo = batchConfigRepo;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateHatchingSeasonTemplateCommand command, Guid? currentUserId, string role)
        {
            if (!CanCreateTemplate(role))
            {
                return ResultModelUtils.FillResult<Guid?>("403", CommonConst.AccessDenied, null);
            }

            var customerId = await ResolveTemplateOwnerCustomerId(role, currentUserId);
            if (role == UserRole.CUSTOMER.ToString() && customerId == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.CustomerNotFound, null);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var template = new HatchingSeasonTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = role == UserRole.CUSTOMER.ToString() ? customerId : null,
                    Name = command.Name,
                    Description = command.Description,
                    TotalDays = command.TotalDays,
                    EggType = command.EggType,
                    IsActive = true,
                    CreatedByType = role == UserRole.CUSTOMER.ToString() ? UserRole.CUSTOMER.ToString() : UserRole.TECHNICIAN.ToString(),
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                };
                await _templateRepo.Add(template);

                foreach (var batchCmd in command.Batches)
                {
                    var batch = new HatchingSeasonTemplateBatch
                    {
                        Id = Guid.NewGuid(),
                        TemplateId = template.Id,
                        BatchIndex = batchCmd.BatchIndex,
                        Name = batchCmd.Name,
                        DayStart = batchCmd.DayStart,
                        DayEnd = batchCmd.DayEnd,
                        Notes = batchCmd.Notes,
                        Status = BaseStatus.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                    };
                    await _batchRepo.Add(batch);

                    foreach (var cfgCmd in batchCmd.Configs)
                    {
                        await _batchConfigRepo.Add(new HatchingSeasonTemplateBatchConfig
                        {
                            Id = Guid.NewGuid(),
                            TemplateBatchId = batch.Id,
                            ConfigId = cfgCmd.ConfigId,
                            TargetValue = cfgCmd.TargetValue,
                            MinValue = cfgCmd.MinValue,
                            MaxValue = cfgCmd.MaxValue,
                            Status = BaseStatus.ACTIVE,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateHatchingSeasonTemplateSuccessfully, template.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating hatching season template");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<HatchingSeasonTemplateDetailResponse?>> GetById(Guid id, Guid? currentUserId, string role)
        {
            var template = await _templateRepo.FindById(id);
            if (template == null)
            {
                return ResultModelUtils.FillResult<HatchingSeasonTemplateDetailResponse?>("404", CommonConst.HatchingSeasonTemplateNotFound, null);
            }

            if (!await CanAccessTemplate(template, currentUserId, role))
            {
                return ResultModelUtils.FillResult<HatchingSeasonTemplateDetailResponse?>("403", CommonConst.AccessDenied, null);
            }

            var batches = await _batchRepo.FindByTemplateId(id);
            var batchDetails = new List<TemplateBatchDetailResponse>();
            foreach (var batch in batches)
            {
                var configs = await _batchConfigRepo.FindByTemplateBatchId(batch.Id);
                batchDetails.Add(new TemplateBatchDetailResponse { Batch = batch, Configs = configs });
            }

            var response = new HatchingSeasonTemplateDetailResponse { Template = template, Batches = batchDetails };
            return ResultModelUtils.FillResult<HatchingSeasonTemplateDetailResponse?>("200", CommonConst.Success, response);
        }

        public async Task<ResultModel<PagedResult<HatchingSeasonTemplate>>> List(Guid? customerId, string? createdByType, Guid? currentUserId, string role, int page, int pageSize)
        {
            Guid? filterCustomerId = customerId;

            if (role == UserRole.CUSTOMER.ToString())
            {
                filterCustomerId = await ResolveTemplateOwnerCustomerId(role, currentUserId);
                if (filterCustomerId == null)
                {
                    return ResultModelUtils.FillResult<PagedResult<HatchingSeasonTemplate>>("404", CommonConst.CustomerNotFound, null);
                }
            }

            var list = await _templateRepo.List(filterCustomerId, createdByType);
            return ResultModelUtils.FillResult<PagedResult<HatchingSeasonTemplate>>("200", CommonConst.Success, PagingUtils.ToPagedResult(list, page, pageSize));
        }

        public async Task<ResultModel<bool>> Update(UpdateHatchingSeasonTemplateCommand command, Guid? currentUserId, string role)
        {
            var template = await _templateRepo.FindById(command.Id);
            if (template == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.HatchingSeasonTemplateNotFound, false);
            }

            if (!await CanModifyTemplate(template, currentUserId, role))
            {
                return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                template.Name = command.Name ?? template.Name;
                template.Description = command.Description ?? template.Description;
                template.TotalDays = command.TotalDays ?? template.TotalDays;
                template.EggType = command.EggType ?? template.EggType;
                template.IsActive = command.IsActive ?? template.IsActive;
                template.UpdatedAt = DateTime.UtcNow;
                template.UpdatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor;

                if (command.Batches != null)
                {
                    var oldBatches = await _batchRepo.FindByTemplateId(command.Id);
                    foreach (var old in oldBatches)
                    {
                        await _batchConfigRepo.SoftDeleteByTemplateBatchId(old.Id);
                    }
                    await _batchRepo.SoftDeleteByTemplateId(command.Id);

                    foreach (var batchCmd in command.Batches)
                    {
                        var batch = new HatchingSeasonTemplateBatch
                        {
                            Id = Guid.NewGuid(),
                            TemplateId = command.Id,
                            BatchIndex = batchCmd.BatchIndex,
                            Name = batchCmd.Name,
                            DayStart = batchCmd.DayStart,
                            DayEnd = batchCmd.DayEnd,
                            Notes = batchCmd.Notes,
                            Status = BaseStatus.ACTIVE,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                        };
                        await _batchRepo.Add(batch);

                        foreach (var cfgCmd in batchCmd.Configs)
                        {
                            await _batchConfigRepo.Add(new HatchingSeasonTemplateBatchConfig
                            {
                                Id = Guid.NewGuid(),
                                TemplateBatchId = batch.Id,
                                ConfigId = cfgCmd.ConfigId,
                                TargetValue = cfgCmd.TargetValue,
                                MinValue = cfgCmd.MinValue,
                                MaxValue = cfgCmd.MaxValue,
                                Status = BaseStatus.ACTIVE,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                            });
                        }
                    }
                }

                await _templateRepo.Update(template);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateHatchingSeasonTemplateSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating template {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> Delete(Guid id, Guid? currentUserId, string role)
        {
            var template = await _templateRepo.FindById(id);
            if (template == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.HatchingSeasonTemplateNotFound, false);

            if (!await CanModifyTemplate(template, currentUserId, role))
                return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);

            var deletedBy = currentUserId?.ToString() ?? CommonConst.SystemActor;
            await _unitOfWork.BeginAsync();
            try
            {
                var batches = await _batchRepo.FindByTemplateId(id);
                foreach (var batch in batches)
                    await _batchConfigRepo.SoftDeleteByTemplateBatchId(batch.Id);
                await _batchRepo.SoftDeleteByTemplateId(id);
                await _templateRepo.SoftDelete(id, deletedBy);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.DeleteSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error deleting template {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        private static bool CanCreateTemplate(string role)
        {
            return role == UserRole.ADMIN.ToString()
                || role == UserRole.TECHNICIAN.ToString()
                || role == UserRole.CUSTOMER.ToString();
        }

        private async Task<Guid?> ResolveTemplateOwnerCustomerId(string role, Guid? currentUserId)
        {
            if (role != UserRole.CUSTOMER.ToString() || !currentUserId.HasValue)
            {
                return null;
            }

            var customer = await _customerRepository.FindByUserId(currentUserId.Value);
            return customer?.Id;
        }

        private async Task<bool> CanAccessTemplate(HatchingSeasonTemplate template, Guid? currentUserId, string role)
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

        private async Task<bool> CanModifyTemplate(HatchingSeasonTemplate template, Guid? currentUserId, string role)
        {
            if (role == UserRole.ADMIN.ToString())
            {
                return true;
            }

            if (role == UserRole.TECHNICIAN.ToString())
            {
                return template.CreatedByType == UserRole.TECHNICIAN.ToString();
            }

            if (role != UserRole.CUSTOMER.ToString() || !currentUserId.HasValue)
            {
                return false;
            }

            var customer = await _customerRepository.FindByUserId(currentUserId.Value);
            return customer != null && template.CustomerId == customer.Id;
        }
    }
}
