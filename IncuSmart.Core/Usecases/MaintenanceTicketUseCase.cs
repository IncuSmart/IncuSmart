namespace IncuSmart.Core.Usecases
{
    public class MaintenanceTicketUseCase : IMaintenanceTicketUseCase
    {
        private readonly IMaintenanceTicketRepository _ticketRepository;
        private readonly IMaintenanceLogRepository _logRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWarrantyRepository _warrantyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MaintenanceTicketUseCase> _logger;

        public MaintenanceTicketUseCase(
            IMaintenanceTicketRepository ticketRepository,
            IMaintenanceLogRepository logRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            IWarrantyRepository warrantyRepository,
            IUnitOfWork unitOfWork,
            ILogger<MaintenanceTicketUseCase> logger)
        {
            _ticketRepository = ticketRepository;
            _logRepository = logRepository;
            _incubatorRepository = incubatorRepository;
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _warrantyRepository = warrantyRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateMaintenanceTicketCommand command, Guid? currentUserId, string role)
        {
            var incubator = await _incubatorRepository.FindById(command.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.IncubatorNotFound, null);
            }

            if (string.IsNullOrWhiteSpace(command.IssueDescription))
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.IssueDescriptionRequired, null);
            }

            Guid? requestedByCustomerId = null;
            if (role == UserRole.CUSTOMER.ToString())
            {
                if (!currentUserId.HasValue || currentUserId == Guid.Empty)
                {
                    return ResultModelUtils.FillResult<Guid?>("401", CommonConst.Unauthorized, null);
                }

                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                {
                    return ResultModelUtils.FillResult<Guid?>("404", CommonConst.CustomerNotFound, null);
                }

                if (incubator.CustomerId != customer.Id)
                {
                    return ResultModelUtils.FillResult<Guid?>("403", CommonConst.AccessDenied, null);
                }

                requestedByCustomerId = customer.Id;
            }

            var warranty = await _warrantyRepository.FindByIncubatorId(command.IncubatorId);
            if (warranty == null || !IsWarrantyActive(warranty))
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.IncubatorNotUnderActiveWarranty, null);
            }

            Guid? technicianId = null;
            var initialStatus = MaintenanceTicketStatus.PENDING;
            DateTime? assignedAt = null;

            if (command.TechnicianId.HasValue && role != UserRole.CUSTOMER.ToString())
            {
                var technicianValidation = await ValidateTechnician(command.TechnicianId.Value);
                if (technicianValidation != null)
                {
                    return technicianValidation;
                }

                technicianId = command.TechnicianId.Value;
                initialStatus = MaintenanceTicketStatus.ASSIGNED;
                assignedAt = DateTime.UtcNow;
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var ticket = new MaintenanceTicket
                {
                    Id = Guid.NewGuid(),
                    IncubatorId = command.IncubatorId,
                    WarrantyId = warranty.Id,
                    RequestedByCustomerId = requestedByCustomerId,
                    TechnicianId = technicianId,
                    IssueDescription = command.IssueDescription.Trim(),
                    AssignedAt = assignedAt,
                    Status = initialStatus,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                };

                await _ticketRepository.Add(ticket);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateMaintenanceTicketSuccessfully, ticket.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating maintenance ticket");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<MaintenanceTicketDetailResponse?>> GetById(Guid id, Guid? currentUserId, string role)
        {
            var ticket = await _ticketRepository.FindById(id);
            if (ticket == null)
            {
                return ResultModelUtils.FillResult<MaintenanceTicketDetailResponse?>("404", CommonConst.MaintenanceTicketNotFound, null);
            }

            if (!await CanAccessTicket(ticket, currentUserId, role))
            {
                return ResultModelUtils.FillResult<MaintenanceTicketDetailResponse?>("403", CommonConst.AccessDenied, null);
            }

            var logs = await _logRepository.FindByTicketId(id);
            var warranty = ticket.WarrantyId.HasValue
                ? await _warrantyRepository.FindByIncubatorId(ticket.IncubatorId)
                : null;

            return ResultModelUtils.FillResult<MaintenanceTicketDetailResponse?>("200", CommonConst.Success, new MaintenanceTicketDetailResponse
            {
                Ticket = ticket,
                Warranty = warranty,
                Logs = logs
            });
        }

        public async Task<ResultModel<PagedResult<MaintenanceTicket>>> List(
            Guid? incubatorId,
            Guid? technicianId,
            string? status,
            Guid? currentUserId,
            string role,
            int page,
            int pageSize)
        {
            Guid? customerId = null;
            Guid? repositoryTechnicianId = technicianId;

            if (role == UserRole.CUSTOMER.ToString())
            {
                if (!currentUserId.HasValue || currentUserId == Guid.Empty)
                {
                    return ResultModelUtils.FillResult<PagedResult<MaintenanceTicket>>("401", CommonConst.Unauthorized, null);
                }

                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                {
                    return ResultModelUtils.FillResult<PagedResult<MaintenanceTicket>>("404", CommonConst.CustomerNotFound, null);
                }

                customerId = customer.Id;
                repositoryTechnicianId = null;
            }

            if (role == UserRole.TECHNICIAN.ToString())
            {
                repositoryTechnicianId = null;
            }

            var list = await _ticketRepository.List(incubatorId, repositoryTechnicianId, customerId, status);

            if (role == UserRole.TECHNICIAN.ToString() && currentUserId.HasValue && currentUserId != Guid.Empty)
            {
                list = list
                    .Where(x => x.Status == MaintenanceTicketStatus.PENDING || x.TechnicianId == currentUserId.Value)
                    .ToList();
            }

            return ResultModelUtils.FillResult<PagedResult<MaintenanceTicket>>("200", CommonConst.Success, PagingUtils.ToPagedResult(list, page, pageSize));
        }

        public async Task<ResultModel<bool>> Assign(AssignMaintenanceTicketCommand command, Guid? currentUserId, string role)
        {
            var ticket = await _ticketRepository.FindById(command.Id);
            if (ticket == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.MaintenanceTicketNotFound, false);
            }

            if (!CanAssignTicket(command.TechnicianId, currentUserId, role))
            {
                return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);
            }

            var technicianValidation = await ValidateTechnician(command.TechnicianId);
            if (technicianValidation != null)
            {
                return ResultModelUtils.FillResult<bool>(
                    technicianValidation.StatusCode,
                    technicianValidation.Message,
                    false);
            }

            if (ticket.Status is MaintenanceTicketStatus.RESOLVED
                or MaintenanceTicketStatus.REJECTED
                or MaintenanceTicketStatus.CANCELLED
                or MaintenanceTicketStatus.CLOSED)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.CannotAssignCompletedTicket, false);
            }

            if (ticket.Status == MaintenanceTicketStatus.IN_PROGRESS)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.CannotReassignTicketInProgress, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                ticket.TechnicianId = command.TechnicianId;
                ticket.AssignedAt ??= DateTime.UtcNow;
                ticket.Status = MaintenanceTicketStatus.ASSIGNED;
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.UpdatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor;

                await _ticketRepository.Update(ticket);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.AssignMaintenanceTicketSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error assigning maintenance ticket {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> UpdateStatus(UpdateMaintenanceTicketStatusCommand command, Guid? currentUserId, string role)
        {
            var ticket = await _ticketRepository.FindById(command.Id);
            if (ticket == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.MaintenanceTicketNotFound, false);
            }

            if (!await CanModifyTicket(ticket, currentUserId, role))
            {
                return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);
            }

            if (!Enum.TryParse<MaintenanceTicketStatus>(command.Status, true, out var nextStatus))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.InvalidMaintenanceTicketStatus, false);
            }

            if (nextStatus == MaintenanceTicketStatus.ASSIGNED)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.UseAssignEndpointToAssignTechnician, false);
            }

            if (!CanTransition(ticket.Status, nextStatus))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.InvalidMaintenanceTicketStatusTransition, false);
            }

            if (nextStatus == MaintenanceTicketStatus.IN_PROGRESS && !ticket.TechnicianId.HasValue)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.TicketMustBeAssignedBeforeProcessing, false);
            }

            if ((nextStatus == MaintenanceTicketStatus.RESOLVED || nextStatus == MaintenanceTicketStatus.REJECTED)
                && string.IsNullOrWhiteSpace(command.ResolutionSummary))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.ResolutionSummaryRequiredForFinalDecision, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                ticket.Status = nextStatus;

                if (!string.IsNullOrWhiteSpace(command.ResolutionSummary))
                {
                    ticket.ResolutionSummary = command.ResolutionSummary.Trim();
                }

                switch (nextStatus)
                {
                    case MaintenanceTicketStatus.IN_PROGRESS:
                        ticket.StartedAt ??= DateTime.UtcNow;
                        break;
                    case MaintenanceTicketStatus.RESOLVED:
                        ticket.ResolvedAt = DateTime.UtcNow;
                        break;
                    case MaintenanceTicketStatus.REJECTED:
                        ticket.RejectedAt = DateTime.UtcNow;
                        ticket.ClosedAt ??= DateTime.UtcNow;
                        break;
                    case MaintenanceTicketStatus.CANCELLED:
                        ticket.CancelledAt = DateTime.UtcNow;
                        ticket.ClosedAt ??= DateTime.UtcNow;
                        break;
                    case MaintenanceTicketStatus.CLOSED:
                        ticket.ClosedAt ??= DateTime.UtcNow;
                        break;
                }

                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.UpdatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor;

                await _ticketRepository.Update(ticket);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateMaintenanceTicketStatusSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating maintenance ticket status {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> Cancel(Guid id, Guid? currentUserId, string role)
        {
            var ticket = await _ticketRepository.FindById(id);
            if (ticket == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.MaintenanceTicketNotFound, false);

            if (ticket.Status is not MaintenanceTicketStatus.PENDING and not MaintenanceTicketStatus.ASSIGNED)
                return ResultModelUtils.FillResult<bool>("400", CommonConst.TicketCannotBeCancelled, false);

            if (role == UserRole.CUSTOMER.ToString() && currentUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null || ticket.RequestedByCustomerId != customer.Id)
                    return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var actor = currentUserId?.ToString() ?? CommonConst.SystemActor;
                ticket.Status = MaintenanceTicketStatus.CANCELLED;
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.UpdatedBy = actor;
                await _ticketRepository.Update(ticket);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.CancelMaintenanceTicketSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error cancelling maintenance ticket {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<Guid?>> AddLog(CreateMaintenanceLogCommand command, Guid? currentUserId, string role)
        {
            var ticket = await _ticketRepository.FindById(command.TicketId);
            if (ticket == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.MaintenanceTicketNotFound, null);
            }

            if (!await CanModifyTicket(ticket, currentUserId, role))
            {
                return ResultModelUtils.FillResult<Guid?>("403", CommonConst.AccessDenied, null);
            }

            if (string.IsNullOrWhiteSpace(command.Description))
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.DescriptionRequired, null);
            }

            // Chỉ gán PerformedByUserId nếu user thực sự tồn tại trong DB
            // (admin hardcoded dùng ID giả, không có trong bảng users)
            Guid? performedByUserId = null;
            if (currentUserId.HasValue && currentUserId.Value != Guid.Empty)
            {
                var performer = await _userRepository.FindById(currentUserId.Value);
                if (performer != null) performedByUserId = currentUserId;
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var log = new MaintenanceLog
                {
                    Id = Guid.NewGuid(),
                    TicketId = command.TicketId,
                    PerformedByUserId = performedByUserId,
                    Description = command.Description.Trim(),
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                };

                await _logRepository.Add(log);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.AddMaintenanceLogSuccessfully, log.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error adding maintenance log for ticket {TicketId}", command.TicketId);
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<List<MaintenanceLog>>> GetLogs(Guid ticketId, Guid? currentUserId, string role)
        {
            var ticket = await _ticketRepository.FindById(ticketId);
            if (ticket == null)
            {
                return ResultModelUtils.FillResult<List<MaintenanceLog>>("404", CommonConst.MaintenanceTicketNotFound, new());
            }

            if (!await CanAccessTicket(ticket, currentUserId, role))
            {
                return ResultModelUtils.FillResult<List<MaintenanceLog>>("403", CommonConst.AccessDenied, new());
            }

            var logs = await _logRepository.FindByTicketId(ticketId);
            return ResultModelUtils.FillResult<List<MaintenanceLog>>("200", CommonConst.Success, logs);
        }

        private async Task<ResultModel<Guid?>?> ValidateTechnician(Guid technicianId)
        {
            var technician = await _userRepository.FindById(technicianId);
            if (technician == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.TechnicianNotFound, null);
            }

            if (technician.Role != UserRole.TECHNICIAN)
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.AssignedUserMustBeTechnician, null);
            }

            return null;
        }

        private async Task<bool> CanAccessTicket(MaintenanceTicket ticket, Guid? currentUserId, string role)
        {
            if (role == UserRole.ADMIN.ToString())
            {
                return true;
            }

            if (role == UserRole.TECHNICIAN.ToString())
            {
                return currentUserId.HasValue
                    && currentUserId != Guid.Empty
                    && ticket.TechnicianId == currentUserId.Value;
            }

            if (role != UserRole.CUSTOMER.ToString() || !currentUserId.HasValue || currentUserId == Guid.Empty)
            {
                return false;
            }

            var customer = await _customerRepository.FindByUserId(currentUserId.Value);
            if (customer == null)
            {
                return false;
            }

            var incubator = await _incubatorRepository.FindById(ticket.IncubatorId);
            return (ticket.RequestedByCustomerId == customer.Id)
                || (incubator != null && incubator.CustomerId == customer.Id);
        }

        private Task<bool> CanModifyTicket(MaintenanceTicket ticket, Guid? currentUserId, string role)
        {
            if (role == UserRole.ADMIN.ToString())
            {
                return Task.FromResult(true);
            }

            if (role != UserRole.TECHNICIAN.ToString() || !currentUserId.HasValue || currentUserId == Guid.Empty)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(ticket.TechnicianId == currentUserId.Value);
        }

        private static bool CanAssignTicket(Guid targetTechnicianId, Guid? currentUserId, string role)
        {
            if (role == UserRole.ADMIN.ToString())
            {
                return true;
            }

            return role == UserRole.TECHNICIAN.ToString()
                && currentUserId.HasValue
                && currentUserId != Guid.Empty
                && currentUserId.Value == targetTechnicianId;
        }

        private static bool CanTransition(MaintenanceTicketStatus currentStatus, MaintenanceTicketStatus nextStatus)
        {
            if (currentStatus == nextStatus)
            {
                return true;
            }

            return currentStatus switch
            {
                MaintenanceTicketStatus.PENDING => nextStatus is MaintenanceTicketStatus.ASSIGNED
                    or MaintenanceTicketStatus.IN_PROGRESS
                    or MaintenanceTicketStatus.REJECTED
                    or MaintenanceTicketStatus.CANCELLED,
                MaintenanceTicketStatus.ASSIGNED => nextStatus is MaintenanceTicketStatus.IN_PROGRESS
                    or MaintenanceTicketStatus.REJECTED
                    or MaintenanceTicketStatus.CANCELLED,
                MaintenanceTicketStatus.IN_PROGRESS => nextStatus is MaintenanceTicketStatus.RESOLVED
                    or MaintenanceTicketStatus.REJECTED
                    or MaintenanceTicketStatus.CANCELLED,
                MaintenanceTicketStatus.RESOLVED => nextStatus == MaintenanceTicketStatus.CLOSED,
                _ => false
            };
        }

        private static bool IsWarrantyActive(Warranty warranty)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (warranty.Status != BaseStatus.ACTIVE)
            {
                return false;
            }

            if (warranty.StartDate.HasValue && warranty.StartDate.Value > today)
            {
                return false;
            }

            if (warranty.EndDate.HasValue && warranty.EndDate.Value < today)
            {
                return false;
            }

            return true;
        }
    }
}
