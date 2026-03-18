using IncuSmart.Core.Services;

namespace IncuSmart.Core.Usecases
{
    public class OrderUseCase : IOrderUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly IIncubatorModelRepository _incubatorModelRepository;
        private readonly IIncubatorModelConfigRepository _incubatorModelConfigRepository;
        private readonly IIncubatorConfigInstanceRepository _incubatorConfigInstanceRepository;
        private readonly IGuestOrderInfoRepository _guestOrderInfoRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisService _redisService;
        private readonly ISMSService _smsService;
        private readonly ILogger<OrderUseCase> _logger;

        public OrderUseCase(
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            ISalesOrderRepository salesOrderRepository,
            IIncubatorRepository incubatorRepository,
            IIncubatorModelRepository incubatorModelRepository,
            IIncubatorModelConfigRepository incubatorModelConfigRepository,
            IIncubatorConfigInstanceRepository incubatorConfigInstanceRepository,
            IGuestOrderInfoRepository guestOrderInfoRepository,
            IUnitOfWork unitOfWork,
            ILogger<OrderUseCase> logger,
            IRedisService redisService,
            ISMSService smsService)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _salesOrderRepository = salesOrderRepository;
            _incubatorRepository = incubatorRepository;
            _incubatorModelRepository = incubatorModelRepository;
            _incubatorModelConfigRepository = incubatorModelConfigRepository;
            _incubatorConfigInstanceRepository = incubatorConfigInstanceRepository;
            _guestOrderInfoRepository = guestOrderInfoRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _redisService = redisService;
            _smsService = smsService;
        }

        public async Task<ResultModel<Guid?>> CreateOrderByCustomer(CreateOrderByCustomerCommand command)
        {
            var customer = await _customerRepository.FindById(command.UserId);
            if (customer == null) return ResultModelUtils.FillResult<Guid?>("404", "User is not existed", null);

            // Add processor/validator để thêm verify dữ liệun vào command nếu cần thiết, tránh việc code bị rối ở đây
            //if (command.Items == null || !command.Items.Any()) return null;

            await _unitOfWork.BeginAsync();
            try
            {
                List<IncubatorModel> incubatorModels = await _incubatorModelRepository.FindByIds(command.Items.Select(x => x.IncubatorModelId).ToList());

                if (command.Items.Select(x => x.IncubatorModelId).ToHashSet().Count() != incubatorModels.Count)
                {
                    return ResultModelUtils.FillResult<Guid?>(
                        "400",
                        "One or more selected products are invalid.",
                        null
                    );
                }

                Guid salesOrderId = Guid.NewGuid();

                var salesOrder = new SalesOrder
                {
                    Id = salesOrderId,
                    OrderCode = "TEST",
                    CustomerId = customer.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.PENDING,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = command.UserId.ToString()
                };

                await _salesOrderRepository.Add(salesOrder);

                foreach (var item in command.Items)
                {
                    var model = await _incubatorModelRepository.FindById(item.IncubatorModelId);
                    if (model == null) continue;

                    var modelConfigs = await _incubatorModelConfigRepository
                        .GetById(item.IncubatorModelId);

                    for (int i = 0; i < item.Quantity; i++)
                    {
                        var incubator = new Incubator
                        {
                            Id = Guid.NewGuid(),
                            QrCode = Guid.NewGuid().ToString(),
                            ModelId = item.IncubatorModelId,
                            CustomerId = customer.Id,
                            Status = IncubatorStatus.PENDING,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = command.UserId.ToString()
                        };

                        await _incubatorRepository.Add(incubator);

                        var configInstances = modelConfigs
                             .SelectMany(modelConfig =>
                                 Enumerable.Range(0, modelConfig.Quantity ?? 1)
                                     .Select(instanceIndex => new IncubatorConfigInstance
                                     {
                                         Id = Guid.NewGuid(),
                                         IncubatorId = incubator.Id,
                                         ConfigId = modelConfig.ConfigId,
                                         InstanceIndex = instanceIndex,
                                         Status = BaseStatus.ACTIVE,
                                         CreatedAt = DateTime.UtcNow,
                                         CreatedBy = command.UserId.ToString()
                                     }))
                             .ToList();

                        await _incubatorConfigInstanceRepository.AddRange(configInstances);

                    }
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Create order successfully", salesOrderId);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                return ResultModelUtils.FillResult<Guid?>("500", e.Message, null);
            }
        }

        public async Task<ResultModel<Guid?>> CreateOrderByGuest(CreateOrderByGuestCommand command)
        {
            // Add processor/validator để thêm verify dữ liệun vào command nếu cần thiết, tránh việc code bị rối ở đây
            //if (command.Items == null || !command.Items.Any()) return null;

            //if (string.IsNullOrWhiteSpace(command.VerificationPass) ||
            //    command.VerificationPass.Length < 6)
            //{
            //    return null;
            //}

            await _unitOfWork.BeginAsync();
            try
            {
                List<IncubatorModel> incubatorModels = await _incubatorModelRepository.FindByIds(command.Items.Select(x => x.IncubatorModelId).ToList()); 

                //if (command.Items.Select(x => x.IncubatorModelId).ToHashSet().Count() != incubatorModels.Count)
                //{
                //    return ResultModelUtils.FillResult<Guid?>(
                //        "400",
                //        "One or more selected products are invalid.",
                //        null
                //    );
                //}

                Guid salesOrderId = Guid.NewGuid();

                var salesOrder = new SalesOrder
                {
                    Id = salesOrderId,
                    OrderCode = "TEST",
                    CustomerId = null,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.PENDING_CLAIM,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "GUEST"
                };

                await _salesOrderRepository.Add(salesOrder);

                var guestOrderInfo = new GuestOrderInfo
                {
                    Id = Guid.NewGuid(),
                    OrderId = salesOrder.Id,
                    FullName = command.FullName,
                    Phone = command.Phone,
                    Email = command.Email,
                    Address = command.Address,
                    Description = command.Description,
                    VerificationPassHash = PasswordUtil.HashPassword(command.VerificationPass),
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "GUEST"
                };

                await _guestOrderInfoRepository.Add(guestOrderInfo);

                foreach (var item in command.Items)
                {
                    var model = await _incubatorModelRepository.FindById(item.IncubatorModelId);
                    if (model == null) continue;

                    var modelConfigs = await _incubatorModelConfigRepository
                        .GetById(item.IncubatorModelId);

                    for (int i = 0; i < item.Quantity; i++)
                    {
                        var incubator = new Incubator
                        {
                            Id = Guid.NewGuid(),
                            QrCode = Guid.NewGuid().ToString(),
                            ModelId = item.IncubatorModelId,
                            CustomerId = null,
                            Status = IncubatorStatus.PENDING_CLAIM,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "GUEST"
                        };

                        await _incubatorRepository.Add(incubator);

                        var configInstances = modelConfigs
                             .SelectMany(modelConfig =>
                                 Enumerable.Range(0, modelConfig.Quantity ?? 1)
                                     .Select(instanceIndex => new IncubatorConfigInstance
                                     {
                                         Id = Guid.NewGuid(),
                                         IncubatorId = incubator.Id,
                                         ConfigId = modelConfig.ConfigId,
                                         InstanceIndex = instanceIndex,
                                         Status = BaseStatus.ACTIVE,
                                         CreatedAt = DateTime.UtcNow,
                                         CreatedBy = "GUEST"
                                     }))
                             .ToList();

                        await _incubatorConfigInstanceRepository.AddRange(configInstances);
                    }
                }

                await _unitOfWork.CommitAsync();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var otp = CodeGenUtils.GenerateNumeric(6);

                        await _redisService.SetAsync(new RedisDto
                        {
                            Key = $"otp:order:{salesOrderId}",
                            Value = otp,
                            Expired = TimeSpan.FromMinutes(5),
                        });

                        await _smsService.SendSMSAsync(new SMSDto
                        {
                            Phone = command.Phone,
                            Content = $"{otp} la ma xac minh don hang cua ban. Ma co hieu luc trong 5 phut.",
                            SmsType = "2",
                            //Brandname = "Baotrixemay",
                            IsUnicode = "0",
                        });

                        _logger.LogInformation("Gửi OTP thành công cho đơn hàng {OrderId}, số điện thoại {Phone}", salesOrderId, command.Phone);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Gửi OTP thất bại cho đơn hàng {OrderId}, số điện thoại {Phone}", salesOrderId, command.Phone);
                    }
                });


                return ResultModelUtils.FillResult<Guid?>(
                    "200",
                    "Create order successfully",
                    salesOrderId
                );
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                return ResultModelUtils.FillResult<Guid?>("500", e.Message, null);
            }
        }
    }
}
