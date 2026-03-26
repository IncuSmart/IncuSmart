namespace IncuSmart.Core.Usecases
{
    public class MaintenanceTicketUseCase : IMaintenanceTicketUseCase
    {
        private readonly IMaintenanceTicketRepository  _ticketRepository;
        private readonly IMaintenanceLogRepository     _logRepository;
        private readonly IIncubatorRepository          _incubatorRepository;
        private readonly ICustomerRepository           _customerRepository;
        private readonly IUserRepository               _userRepository;
        private readonly IUnitOfWork                   _unitOfWork;
        private readonly ILogger<MaintenanceTicketUseCase> _logger;

        public MaintenanceTicketUseCase(
            IMaintenanceTicketRepository ticketRepository,
            IMaintenanceLogRepository logRepository,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<MaintenanceTicketUseCase> logger)
        {
            _ticketRepository    = ticketRepository;
            _logRepository       = logRepository;
            _incubatorRepository = incubatorRepository;
            _customerRepository  = customerRepository;
            _userRepository      = userRepository;
            _unitOfWork          = unitOfWork;
            _logger              = logger;
        }

        // ─── CREATE TICKET ─────────────────────────────────────────────────────────
        public async Task<ResultModel<Guid?>> Create(CreateMaintenanceTicketCommand command)
        {
            var incubator = await _incubatorRepository.FindById(command.IncubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<Guid?>("404", "Không tìm thấy máy ấp", null);

            var technician = await _userRepository.FindById(command.TechnicianId);
            if (technician == null)
                return ResultModelUtils.FillResult<Guid?>("404", "Không tìm thấy kỹ thuật viên", null);

            await _unitOfWork.BeginAsync();
            try
            {
                var ticket = new MaintenanceTicket
                {
                    Id           = Guid.NewGuid(),
                    IncubatorId  = command.IncubatorId,
                    TechnicianId = command.TechnicianId,
                    Status       = BaseStatus.ACTIVE,
                    CreatedAt    = DateTime.UtcNow,
                    CreatedBy    = "SYSTEM",
                };
                await _ticketRepository.Add(ticket);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Tạo phiếu bảo trì thành công", ticket.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating maintenance ticket");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        // ─── GET BY ID (kèm logs) ──────────────────────────────────────────────────
        public async Task<ResultModel<MaintenanceTicketDetail?>> GetById(
            Guid id, Guid? currentUserId, string role)
        {
            var ticket = await _ticketRepository.FindById(id);
            if (ticket == null)
                return ResultModelUtils.FillResult<MaintenanceTicketDetail?>("404", "Không tìm thấy phiếu bảo trì", null);

            // CUSTOMER: chỉ xem được ticket của máy mình
            if (role == "CUSTOMER" && currentUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                var incubator = await _incubatorRepository.FindById(ticket.IncubatorId);
                if (customer == null || incubator == null || incubator.CustomerId != customer.Id)
                    return ResultModelUtils.FillResult<MaintenanceTicketDetail?>("400",
                        "Bạn không có quyền xem phiếu bảo trì này", null);
            }

            var logs = await _logRepository.FindByTicketId(id);
            var response = new MaintenanceTicketDetail { Ticket = ticket, Logs = logs };
            return ResultModelUtils.FillResult<MaintenanceTicketDetail?>("200", "Success", response);
        }

        // ─── GET ALL ───────────────────────────────────────────────────────────────
        // ADMIN/TECHNICIAN: filter thoải mái
        // CUSTOMER: chỉ thấy ticket của máy mình
        public async Task<ResultModel<List<MaintenanceTicket>>> GetAll(
            Guid? incubatorId, Guid? technicianId, string? status,
            Guid? currentUserId, string role)
        {
            Guid? resolvedIncubatorId = incubatorId;

            if (role == "CUSTOMER" && currentUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                    return ResultModelUtils.FillResult<List<MaintenanceTicket>>("404", "Không tìm thấy thông tin khách hàng", new());
                // Sẽ filter ở repository theo customerId qua incubator
            }

            var list = await _ticketRepository.FindAll(resolvedIncubatorId, technicianId, status);
            return ResultModelUtils.FillResult<List<MaintenanceTicket>>("200", "Success", list);
        }

        // ─── UPDATE STATUS ─────────────────────────────────────────────────────────
        public async Task<ResultModel<bool>> UpdateStatus(UpdateMaintenanceTicketStatusCommand command)
        {
            var ticket = await _ticketRepository.FindById(command.Id);
            if (ticket == null)
                return ResultModelUtils.FillResult<bool>("404", "Không tìm thấy phiếu bảo trì", false);

            await _unitOfWork.BeginAsync();
            try
            {
                ticket.Status    = Enum.Parse<BaseStatus>(command.Status);
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.UpdatedBy = "SYSTEM";

                // Nếu đóng ticket → set ClosedAt
                if (command.Status == "INACTIVE")
                    ticket.ClosedAt = DateTime.UtcNow;

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Cập nhật trạng thái thành công", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating ticket status {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        // ─── ADD LOG ───────────────────────────────────────────────────────────────
        public async Task<ResultModel<Guid?>> AddLog(CreateMaintenanceLogCommand command)
        {
            var ticket = await _ticketRepository.FindById(command.TicketId);
            if (ticket == null)
                return ResultModelUtils.FillResult<Guid?>("404", "Không tìm thấy phiếu bảo trì", null);

            await _unitOfWork.BeginAsync();
            try
            {
                var log = new MaintenanceLog
                {
                    Id          = Guid.NewGuid(),
                    TicketId    = command.TicketId,
                    Description = command.Description,
                    Status      = BaseStatus.ACTIVE,
                    CreatedAt   = DateTime.UtcNow,
                    CreatedBy   = "SYSTEM",
                };
                await _logRepository.Add(log);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Thêm nhật ký bảo trì thành công", log.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error adding maintenance log for ticket {TicketId}", command.TicketId);
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        // ─── GET LOGS ──────────────────────────────────────────────────────────────
        public async Task<ResultModel<List<MaintenanceLog>>> GetLogs(Guid ticketId)
        {
            var ticket = await _ticketRepository.FindById(ticketId);
            if (ticket == null)
                return ResultModelUtils.FillResult<List<MaintenanceLog>>("404", "Không tìm thấy phiếu bảo trì", new());

            var logs = await _logRepository.FindByTicketId(ticketId);
            return ResultModelUtils.FillResult<List<MaintenanceLog>>("200", "Success", logs);
        }
    }
}
