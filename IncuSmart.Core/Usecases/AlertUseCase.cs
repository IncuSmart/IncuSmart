namespace IncuSmart.Core.Usecases
{
    public class AlertUseCase : IAlertUseCase
    {
        private readonly IAlertRepository _alertRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AlertUseCase> _logger;

        public AlertUseCase(
            IAlertRepository alertRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<AlertUseCase> logger)
        {
            _alertRepository = alertRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Alert?>> GetById(Guid id, Guid? currentUserId, string role)
        {
            var alert = await _alertRepository.FindById(id);
            if (alert == null)
            {
                return ResultModelUtils.FillResult<Alert?>("404", "Alert not found", null);
            }

            if (role == UserRole.CUSTOMER.ToString() && currentUserId.HasValue && currentUserId != Guid.Empty)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                {
                    return ResultModelUtils.FillResult<Alert?>("404", CommonConst.CustomerNotFound, null);
                }

                var customerAlerts = await _alertRepository.ListByCustomer(customer.Id, alert.IncubatorId, null, null, null, null);
                if (!customerAlerts.Any(a => a.Id == id))
                {
                    return ResultModelUtils.FillResult<Alert?>("403", CommonConst.AccessDenied, null);
                }
            }

            return ResultModelUtils.FillResult<Alert?>("200", CommonConst.Success, alert);
        }

        public async Task<ResultModel<PagedResult<Alert>>> List(
            Guid? incubatorId,
            string? severity,
            string? status,
            DateTime? from,
            DateTime? to,
            Guid? currentUserId,
            string role,
            int page,
            int pageSize)
        {
            List<Alert> list;

            if (role == UserRole.CUSTOMER.ToString() && currentUserId.HasValue && currentUserId != Guid.Empty)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                {
                    return ResultModelUtils.FillResult<PagedResult<Alert>>("404", CommonConst.CustomerNotFound, null);
                }

                list = await _alertRepository.ListByCustomer(customer.Id, incubatorId, severity, status, from, to);
            }
            else
            {
                list = await _alertRepository.List(incubatorId, severity, status, from, to);
            }

            return ResultModelUtils.FillResult<PagedResult<Alert>>("200", CommonConst.Success, PagingUtils.ToPagedResult(list, page, pageSize));
        }

        public async Task<ResultModel<bool>> Resolve(ResolveAlertCommand command)
        {
            var alert = await _alertRepository.FindById(command.Id);
            if (alert == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.AlertNotFound, false);
            }

            if (alert.Status != AlertStatus.OPEN)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.AlertAlreadyResolved, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                alert.Message = command.Message ?? alert.Message;
                alert.ResolvedBy = command.ResolvedBy;
                alert.Status = AlertStatus.RESOLVED_MANUAL;
                alert.UpdatedAt = DateTime.UtcNow;
                alert.UpdatedBy = command.UpdatedBy ?? CommonConst.SystemActor;

                await _alertRepository.Update(alert);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.ResolveAlertSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error resolving alert {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }
}
