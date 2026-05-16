namespace IncuSmart.Core.Usecases
{
    public class CustomerUseCase : ICustomerUseCase
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerUseCase> _logger;

        public CustomerUseCase(
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            ISalesOrderRepository salesOrderRepository,
            IUnitOfWork unitOfWork,
            ILogger<CustomerUseCase> logger)
        {
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _salesOrderRepository = salesOrderRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<CustomerDetailResponse?>> GetById(Guid id)
        {
            var customer = await _customerRepository.FindById(id);
            if (customer == null)
            {
                return ResultModelUtils.FillResult<CustomerDetailResponse?>("404", CommonConst.CustomerNotFound, null);
            }

            var user = await _userRepository.FindById(customer.UserId);
            if (user == null)
            {
                return ResultModelUtils.FillResult<CustomerDetailResponse?>("404", CommonConst.UserNotFound, null);
            }

            return ResultModelUtils.FillResult<CustomerDetailResponse?>("200", CommonConst.Success, BuildDetail(customer, user));
        }

        public async Task<ResultModel<CustomerDetailResponse?>> GetByUserId(Guid userId)
        {
            var customer = await _customerRepository.FindByUserId(userId);
            if (customer == null)
            {
                return ResultModelUtils.FillResult<CustomerDetailResponse?>("404", CommonConst.CustomerNotFound, null);
            }

            var user = await _userRepository.FindById(userId);
            if (user == null)
            {
                return ResultModelUtils.FillResult<CustomerDetailResponse?>("404", CommonConst.UserNotFound, null);
            }

            return ResultModelUtils.FillResult<CustomerDetailResponse?>("200", CommonConst.Success, BuildDetail(customer, user));
        }

        public async Task<ResultModel<PagedResult<CustomerSummaryResponse>>> List(string? status, string? search, int page, int pageSize)
        {
            var customers = await _customerRepository.List(status, search);
            var users = await _userRepository.FindByIds(customers.Select(x => x.UserId).Distinct().ToList());
            var userLookup = users.ToDictionary(x => x.Id, x => x);

            var results = customers
                .Where(x => userLookup.ContainsKey(x.UserId))
                .Select(x => BuildSummary(x, userLookup[x.UserId]))
                .ToList();

            return ResultModelUtils.FillResult<PagedResult<CustomerSummaryResponse>>("200", CommonConst.Success, PagingUtils.ToPagedResult(results, page, pageSize));
        }

        public async Task<ResultModel<List<SalesOrder>>> GetOrders(Guid customerId)
        {
            var customer = await _customerRepository.FindById(customerId);
            if (customer == null)
            {
                return ResultModelUtils.FillResult<List<SalesOrder>>("404", CommonConst.CustomerNotFound, null);
            }

            var orders = await _salesOrderRepository.FindByCustomerId(customerId);
            return ResultModelUtils.FillResult<List<SalesOrder>>("200", CommonConst.Success, orders);
        }

        public async Task<ResultModel<bool>> UpdateProfile(UpdateCustomerProfileCommand command)
        {
            var customer = await _customerRepository.FindById(command.CustomerId);
            if (customer == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.CustomerNotFound, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                customer.Address = command.Address ?? customer.Address;
                customer.UpdatedAt = DateTime.UtcNow;
                customer.UpdatedBy = CommonConst.SystemActor;

                await _customerRepository.Update(customer);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating customer profile {CustomerId}", command.CustomerId);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        private static CustomerSummaryResponse BuildSummary(Customer customer, User user)
        {
            return new CustomerSummaryResponse
            {
                Id = customer.Id,
                UserId = customer.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = customer.Address,
                UserStatus = user.Status.ToString(),
                CustomerStatus = customer.Status.ToString(),
                Role = user.Role.ToString()
            };
        }

        private static CustomerDetailResponse BuildDetail(Customer customer, User user)
        {
            return new CustomerDetailResponse
            {
                Id = customer.Id,
                UserId = customer.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = customer.Address,
                UserStatus = user.Status.ToString(),
                CustomerStatus = customer.Status.ToString(),
                Role = user.Role.ToString(),
                UserCreatedAt = user.CreatedAt,
                UserUpdatedAt = user.UpdatedAt,
                CustomerCreatedAt = customer.CreatedAt,
                CustomerUpdatedAt = customer.UpdatedAt
            };
        }
    }
}
