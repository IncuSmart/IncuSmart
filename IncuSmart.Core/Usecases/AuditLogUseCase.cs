namespace IncuSmart.Core.Usecases
{
    public class AuditLogUseCase : IAuditLogUseCase
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuditLogUseCase> _logger;

        public AuditLogUseCase(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork,
            ILogger<AuditLogUseCase> logger)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateAuditLogCommand command)
        {
            if (command.UserId == Guid.Empty)
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.UserIdRequired, null);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = command.UserId,
                    Action = command.Action,
                    Entity = command.Entity,
                    EntityId = command.EntityId,
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = command.UserId.ToString()
                };

                await _auditLogRepository.Add(auditLog);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateAuditLogSuccessfully, auditLog.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating audit log for user {UserId}", command.UserId);
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<AuditLog?>> GetById(Guid id)
        {
            var auditLog = await _auditLogRepository.FindById(id);
            return auditLog == null
                ? ResultModelUtils.FillResult<AuditLog?>("404", CommonConst.AuditLogNotFound, null)
                : ResultModelUtils.FillResult<AuditLog?>("200", CommonConst.Success, auditLog);
        }

        public async Task<ResultModel<PagedResult<AuditLog>>> List(Guid? userId, string? action, string? entity, int page, int pageSize)
        {
            AuditAction? auditAction = null;
            AuditEntityType? auditEntity = null;

            if (!string.IsNullOrWhiteSpace(action) &&
                Enum.TryParse<AuditAction>(action, true, out var parsedAction))
            {
                auditAction = parsedAction;
            }

            if (!string.IsNullOrWhiteSpace(entity) &&
                Enum.TryParse<AuditEntityType>(entity, true, out var parsedEntity))
            {
                auditEntity = parsedEntity;
            }

            var list = await _auditLogRepository.List(userId, auditAction, auditEntity);
            return ResultModelUtils.FillResult<PagedResult<AuditLog>>("200", CommonConst.Success, PagingUtils.ToPagedResult(list, page, pageSize));
        }
    }
}
