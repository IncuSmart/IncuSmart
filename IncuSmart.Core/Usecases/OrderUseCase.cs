namespace IncuSmart.Core.Usecases
{
    public class OrderUseCase : IOrderUseCase
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly ISalesOrderItemRepository _salesOrderItemRepository;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly IIncubatorModelRepository _incubatorModelRepository;
        private readonly IGuestOrderInfoRepository _guestOrderInfoRepository;
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderUseCase> _logger;

        public OrderUseCase(
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            ISalesOrderRepository salesOrderRepository,
            ISalesOrderItemRepository salesOrderItemRepository,
            IIncubatorRepository incubatorRepository,
            IIncubatorModelRepository incubatorModelRepository,
            IGuestOrderInfoRepository guestOrderInfoRepository,
            IPaymentGatewayService paymentGatewayService,
            IUnitOfWork unitOfWork,
            ILogger<OrderUseCase> logger)
        {
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _salesOrderRepository = salesOrderRepository;
            _salesOrderItemRepository = salesOrderItemRepository;
            _incubatorRepository = incubatorRepository;
            _incubatorModelRepository = incubatorModelRepository;
            _guestOrderInfoRepository = guestOrderInfoRepository;
            _paymentGatewayService = paymentGatewayService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<CreateOrderResponse?>> CreateOrderByCustomer(CreateOrderByCustomerCommand command)
        {
            var customer = await _customerRepository.FindByUserId(command.UserId);
            if (customer == null)
            {
                return ResultModelUtils.FillResult<CreateOrderResponse?>("404", CommonConst.CustomerNotFound, null);
            }

            var user = await _userRepository.FindById(command.UserId);
            if (user == null)
            {
                return ResultModelUtils.FillResult<CreateOrderResponse?>("404", CommonConst.UserNotFound, null);
            }

            var preparedOrder = await PrepareOrderItems(command.Items);
            if (preparedOrder.Error != null)
            {
                return ResultModelUtils.FillResult<CreateOrderResponse?>(
                    preparedOrder.Error.StatusCode,
                    preparedOrder.Error.Message,
                    null);
            }

            var paymentOrderCode = GeneratePaymentOrderCode();
            var salesOrderId = Guid.NewGuid();
            var salesOrder = new SalesOrder
            {
                Id = salesOrderId,
                OrderCode = GenerateOrderCode(),
                CustomerId = customer.Id,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = customer.Address ?? string.Empty,
                TotalAmount = preparedOrder.TotalAmount,
                PaymentStatus = PaymentStatus.PENDING,
                PaymentOrderCode = paymentOrderCode,
                Status = OrderStatus.PENDING,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.UserId.ToString()
            };

            await _unitOfWork.BeginAsync();
            try
            {
                await _salesOrderRepository.Add(salesOrder);
                await _unitOfWork.SaveChangesAsync();   // flush order trước để FK hợp lệ

                await _salesOrderItemRepository.AddRange(preparedOrder.OrderItems.Select(x =>
                {
                    x.OrderId = salesOrderId;
                    x.CreatedBy = command.UserId.ToString();
                    return x;
                }).ToList());

                var paymentLink = await _paymentGatewayService.CreatePaymentLink(new PaymentLinkRequest
                {
                    OrderCode = paymentOrderCode,
                    Amount = preparedOrder.TotalAmount,
                    Description = BuildPaymentDescription(salesOrder.OrderCode),
                    BuyerName = user.FullName,
                    BuyerEmail = user.Email,
                    BuyerPhone = user.Phone,
                    BuyerAddress = customer.Address,
                    Items = preparedOrder.PaymentItems
                });

                ApplyPaymentLink(salesOrder, paymentLink, command.UserId.ToString());
                await _salesOrderRepository.Update(salesOrder);

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult("200", CommonConst.CreateOrderAndPaymentLinkSuccessfully, ToCreateOrderResponse(salesOrder));
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(e, "Error creating customer order for user {UserId}", command.UserId);
                return ResultModelUtils.FillResult<CreateOrderResponse?>("500", e.Message, null);
            }
        }

        public async Task<ResultModel<CreateOrderResponse?>> CreateOrderByGuest(CreateOrderByGuestCommand command)
        {
            var preparedOrder = await PrepareOrderItems(command.Items);
            if (preparedOrder.Error != null)
            {
                return ResultModelUtils.FillResult<CreateOrderResponse?>(
                    preparedOrder.Error.StatusCode,
                    preparedOrder.Error.Message,
                    null);
            }

            var paymentOrderCode = GeneratePaymentOrderCode();
            var salesOrderId = Guid.NewGuid();
            var salesOrder = new SalesOrder
            {
                Id = salesOrderId,
                OrderCode = GenerateOrderCode(),
                CustomerId = null,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = command.ShippingAddress ?? string.Empty,
                TotalAmount = preparedOrder.TotalAmount,
                PaymentStatus = PaymentStatus.PENDING,
                PaymentOrderCode = paymentOrderCode,
                Status = OrderStatus.PENDING,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CommonConst.GuestActor
            };

            await _unitOfWork.BeginAsync();
            try
            {
                await _salesOrderRepository.Add(salesOrder);
                await _unitOfWork.SaveChangesAsync();   // flush order trước để FK hợp lệ

                var guestOrderInfo = new GuestOrderInfo
                {
                    Id = Guid.NewGuid(),
                    OrderId = salesOrderId,
                    FullName = command.FullName,
                    Phone = command.Phone,
                    Email = command.Email,
                    Description = command.Description,
                    VerificationPassHash = PasswordUtil.HashPassword(command.VerificationPass),
                    Status = GuestOrderStatus.PENDING_VERIFICATION,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CommonConst.GuestActor
                };

                await _guestOrderInfoRepository.Add(guestOrderInfo);
                await _salesOrderItemRepository.AddRange(preparedOrder.OrderItems.Select(x =>
                {
                    x.OrderId = salesOrderId;
                    x.CreatedBy = CommonConst.GuestActor;
                    return x;
                }).ToList());

                var paymentLink = await _paymentGatewayService.CreatePaymentLink(new PaymentLinkRequest
                {
                    OrderCode = paymentOrderCode,
                    Amount = preparedOrder.TotalAmount,
                    Description = BuildPaymentDescription(salesOrder.OrderCode),
                    BuyerName = command.FullName,
                    BuyerEmail = command.Email,
                    BuyerPhone = command.Phone,
                    BuyerAddress = command.ShippingAddress,
                    Items = preparedOrder.PaymentItems
                });

                ApplyPaymentLink(salesOrder, paymentLink, CommonConst.GuestActor);
                await _salesOrderRepository.Update(salesOrder);

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult("200", CommonConst.CreateOrderAndPaymentLinkSuccessfully, ToCreateOrderResponse(salesOrder));
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(e, "Error creating guest order");
                return ResultModelUtils.FillResult<CreateOrderResponse?>("500", e.Message, null);
            }
        }

        public async Task<ResultModel<CreateOrderResponse?>> CreateOrderBySales(CreateOrderBySalesCommand command)
        {
            var customer = await _customerRepository.FindById(command.CustomerId);
            if (customer == null)
                return ResultModelUtils.FillResult<CreateOrderResponse?>("404", CommonConst.CustomerNotFound, null);

            var user = await _userRepository.FindById(customer.UserId);
            if (user == null)
                return ResultModelUtils.FillResult<CreateOrderResponse?>("404", CommonConst.UserNotFound, null);

            var preparedOrder = await PrepareOrderItems(command.Items);
            if (preparedOrder.Error != null)
                return ResultModelUtils.FillResult<CreateOrderResponse?>(preparedOrder.Error.StatusCode, preparedOrder.Error.Message, null);

            var paymentOrderCode = GeneratePaymentOrderCode();
            var salesOrderId = Guid.NewGuid();
            var actor = command.CreatedByUserId.ToString();
            var salesOrder = new SalesOrder
            {
                Id = salesOrderId,
                OrderCode = GenerateOrderCode(),
                CustomerId = customer.Id,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = customer.Address ?? string.Empty,
                TotalAmount = preparedOrder.TotalAmount,
                PaymentStatus = PaymentStatus.PENDING,
                PaymentOrderCode = paymentOrderCode,
                Status = OrderStatus.PENDING,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actor
            };

            await _unitOfWork.BeginAsync();
            try
            {
                await _salesOrderRepository.Add(salesOrder);
                await _unitOfWork.SaveChangesAsync();   // flush order trước để FK hợp lệ

                await _salesOrderItemRepository.AddRange(preparedOrder.OrderItems.Select(x =>
                {
                    x.OrderId = salesOrderId;
                    x.CreatedBy = actor;
                    return x;
                }).ToList());

                var paymentLink = await _paymentGatewayService.CreatePaymentLink(new PaymentLinkRequest
                {
                    OrderCode = paymentOrderCode,
                    Amount = preparedOrder.TotalAmount,
                    Description = BuildPaymentDescription(salesOrder.OrderCode),
                    BuyerName = user.FullName,
                    BuyerEmail = user.Email,
                    BuyerPhone = user.Phone,
                    BuyerAddress = customer.Address,
                    Items = preparedOrder.PaymentItems
                });

                ApplyPaymentLink(salesOrder, paymentLink, actor);
                await _salesOrderRepository.Update(salesOrder);

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult("200", CommonConst.CreateOrderAndPaymentLinkSuccessfully, ToCreateOrderResponse(salesOrder));
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(e, "Error creating sales order for customer {CustomerId}", command.CustomerId);
                return ResultModelUtils.FillResult<CreateOrderResponse?>("500", e.Message, null);
            }
        }

        public async Task<ResultModel<bool>> AssignIncubatorToOrderItem(AssignIncubatorToOrderItemCommand command)
        {
            var order = await _salesOrderRepository.FindById(command.OrderId);
            if (order == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.OrderNotFound, false);
            }

            if (order.Status is OrderStatus.CANCELLED or OrderStatus.COMPLETED)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderCannotBeUpdated, false);
            }

            if (order.PaymentStatus != PaymentStatus.PAID)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderPaymentNotCompleted, false);
            }

            var orderItem = await _salesOrderItemRepository.FindById(command.OrderItemId);
            if (orderItem == null || orderItem.OrderId != command.OrderId)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.OrderItemNotFound, false);
            }

            if (orderItem.Status != OrderItemStatus.PENDING_ASSIGNMENT)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderItemAlreadyAssigned, false);
            }

            var incubator = await _incubatorRepository.FindById(command.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorNotFound, false);
            }

            if (incubator.ModelId != orderItem.IncubatorModelId)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.IncubatorModelDoesNotMatchOrderItem, false);
            }

            if (incubator.Status != IncubatorStatus.AVAILABLE || incubator.CustomerId != null)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.IncubatorNotAvailableForAssignment, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                orderItem.IncubatorId = incubator.Id;
                orderItem.Status = OrderItemStatus.ASSIGNED;
                orderItem.UpdatedAt = DateTime.UtcNow;
                orderItem.UpdatedBy = CommonConst.SystemActor;
                await _salesOrderItemRepository.Update(orderItem);

                incubator.Status = IncubatorStatus.RESERVED;
                incubator.UpdatedAt = DateTime.UtcNow;
                incubator.UpdatedBy = CommonConst.SystemActor;
                await _incubatorRepository.Update(incubator);

                if (order.Status == OrderStatus.PENDING)
                {
                    order.Status = OrderStatus.PROCESSING;
                    order.UpdatedAt = DateTime.UtcNow;
                    order.UpdatedBy = CommonConst.SystemActor;
                    await _salesOrderRepository.Update(order);
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.AssignIncubatorSuccessfully, true);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(e, "Error assigning incubator {IncubatorId} to order item {OrderItemId}", command.IncubatorId, command.OrderItemId);
                return ResultModelUtils.FillResult<bool>("500", e.Message, false);
            }
        }

        public async Task<ResultModel<bool>> CompleteOrder(CompleteOrderCommand command)
        {
            var order = await _salesOrderRepository.FindById(command.OrderId);
            if (order == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.OrderNotFound, false);
            }

            if (order.PaymentStatus != PaymentStatus.PAID)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderPaymentNotCompleted, false);
            }

            if (order.Status == OrderStatus.CANCELLED)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.CancelledOrderCannotBeCompleted, false);
            }

            if (order.Status == OrderStatus.COMPLETED)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderAlreadyCompleted, false);
            }

            var orderItems = await _salesOrderItemRepository.FindByOrderId(command.OrderId);
            if (!orderItems.Any())
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderHasNoItems, false);
            }

            if (orderItems.Any(x => x.Status != OrderItemStatus.ASSIGNED || x.IncubatorId == null))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.AllSalesOrderItemsMustBeAssignedBeforeCompletion, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                foreach (var orderItem in orderItems)
                {
                    var incubator = await _incubatorRepository.FindById(orderItem.IncubatorId!.Value)
                        ?? throw new InvalidOperationException($"Assigned incubator {orderItem.IncubatorId} was not found.");

                    if (order.CustomerId.HasValue)
                    {
                        incubator.CustomerId = order.CustomerId;
                        incubator.Status = IncubatorStatus.ACTIVE;
                        incubator.ActivatedAt ??= DateTime.UtcNow;
                    }
                    else
                    {
                        incubator.Status = IncubatorStatus.RESERVED;
                    }

                    incubator.UpdatedAt = DateTime.UtcNow;
                    incubator.UpdatedBy = CommonConst.SystemActor;
                    await _incubatorRepository.Update(incubator);
                }

                order.Status = OrderStatus.COMPLETED;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = CommonConst.SystemActor;
                await _salesOrderRepository.Update(order);

                var guestOrderInfo = await _guestOrderInfoRepository.FindByOrderId(order.Id);
                if (guestOrderInfo != null && guestOrderInfo.Status == GuestOrderStatus.PENDING_VERIFICATION)
                {
                    guestOrderInfo.Status = GuestOrderStatus.VERIFIED;
                    guestOrderInfo.UpdatedAt = DateTime.UtcNow;
                    guestOrderInfo.UpdatedBy = CommonConst.SystemActor;
                    await _guestOrderInfoRepository.Update(guestOrderInfo);
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.CompleteOrderSuccessfully, true);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(e, "Error completing order {OrderId}", command.OrderId);
                return ResultModelUtils.FillResult<bool>("500", e.Message, false);
            }
        }

        public async Task<ResultModel<bool>> ClaimGuestOrder(ClaimGuestOrderCommand command)
        {
            var customer = await _customerRepository.FindByUserId(command.UserId);
            if (customer == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.CustomerNotFound, false);
            }

            var order = await _salesOrderRepository.FindById(command.OrderId);
            if (order == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.OrderNotFound, false);
            }

            if (order.CustomerId.HasValue)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderAlreadyClaimed, false);
            }

            if (order.Status != OrderStatus.COMPLETED)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OnlyCompletedGuestOrdersCanBeClaimed, false);
            }

            var guestOrderInfo = await _guestOrderInfoRepository.FindByOrderId(command.OrderId);
            if (guestOrderInfo == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.GuestOrderInformationNotFound, false);
            }

            if (guestOrderInfo.Status == GuestOrderStatus.CLAIMED)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderAlreadyClaimed, false);
            }

            if (guestOrderInfo.Status != GuestOrderStatus.VERIFIED)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.GuestOrderNotReadyToBeClaimed, false);
            }

            if (!PasswordUtil.VerifyPassword(command.VerificationPass, guestOrderInfo.VerificationPassHash))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.InvalidVerificationPass, false);
            }

            var orderItems = await _salesOrderItemRepository.FindByOrderId(command.OrderId);
            if (orderItems.Any(x => x.Status != OrderItemStatus.ASSIGNED || x.IncubatorId == null))
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderMissingAssignedIncubators, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                order.CustomerId = customer.Id;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = command.UserId.ToString();
                await _salesOrderRepository.Update(order);

                foreach (var orderItem in orderItems)
                {
                    var incubator = await _incubatorRepository.FindById(orderItem.IncubatorId!.Value)
                        ?? throw new InvalidOperationException($"Assigned incubator {orderItem.IncubatorId} was not found.");

                    incubator.CustomerId = customer.Id;
                    incubator.Status = IncubatorStatus.ACTIVE;
                    incubator.ActivatedAt ??= DateTime.UtcNow;
                    incubator.UpdatedAt = DateTime.UtcNow;
                    incubator.UpdatedBy = command.UserId.ToString();
                    await _incubatorRepository.Update(incubator);
                }

                guestOrderInfo.Status = GuestOrderStatus.CLAIMED;
                guestOrderInfo.ClaimedAt = DateTime.UtcNow;
                guestOrderInfo.ClaimedBy = command.UserId;
                guestOrderInfo.UpdatedAt = DateTime.UtcNow;
                guestOrderInfo.UpdatedBy = command.UserId.ToString();
                await _guestOrderInfoRepository.Update(guestOrderInfo);

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.ClaimGuestOrderSuccessfully, true);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(e, "Error claiming guest order {OrderId}", command.OrderId);
                return ResultModelUtils.FillResult<bool>("500", e.Message, false);
            }
        }

        public async Task<ResultModel<bool>> HandlePaymentWebhook(HandleOrderPaymentWebhookCommand command)
        {
            var order = await _salesOrderRepository.FindByPaymentOrderCode(command.PaymentOrderCode);
            if (order == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.PaymentOrderNotFound, false);
            }

            if (order.TotalAmount != command.Amount)
            {
                return ResultModelUtils.FillResult<bool>("400", CommonConst.PaymentWebhookInvalid, false);
            }

            if (order.PaymentStatus == PaymentStatus.PAID)
            {
                return ResultModelUtils.FillResult<bool>("200", CommonConst.PaymentWebhookProcessedSuccessfully, true);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                order.PaymentLinkId ??= command.PaymentLinkId;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = CommonConst.SystemActor;

                if (command.Success && string.Equals(command.ProviderCode, "00", StringComparison.OrdinalIgnoreCase))
                {
                    order.PaymentStatus = PaymentStatus.PAID;
                    order.PaidAt ??= DateTime.UtcNow;
                }
                else
                {
                    order.PaymentStatus = PaymentStatus.FAILED;
                }

                await _salesOrderRepository.Update(order);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.PaymentWebhookProcessedSuccessfully, true);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(e, "Error handling payment webhook for payOS order {PaymentOrderCode}", command.PaymentOrderCode);
                return ResultModelUtils.FillResult<bool>("500", e.Message, false);
            }
        }

        public async Task<ResultModel<bool>> CancelOrder(CancelOrderCommand command)
        {
            var order = await _salesOrderRepository.FindById(command.OrderId);
            if (order == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.OrderNotFound, false);

            if (order.Status is OrderStatus.COMPLETED or OrderStatus.CANCELLED)
                return ResultModelUtils.FillResult<bool>("400", CommonConst.OrderCannotBeCancelled, false);

            if (command.Role == UserRole.CUSTOMER.ToString() && command.CancelledByUserId.HasValue)
            {
                var customer = await _customerRepository.FindByUserId(command.CancelledByUserId.Value);
                if (customer == null || order.CustomerId != customer.Id)
                    return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);
            }

            var orderItems = await _salesOrderItemRepository.FindByOrderId(command.OrderId);

            await _unitOfWork.BeginAsync();
            try
            {
                var now = DateTime.UtcNow;
                var actor = command.CancelledByUserId?.ToString() ?? CommonConst.SystemActor;

                foreach (var item in orderItems)
                {
                    if (item.IncubatorId.HasValue)
                    {
                        var incubator = await _incubatorRepository.FindById(item.IncubatorId.Value);
                        if (incubator != null && incubator.Status == IncubatorStatus.RESERVED)
                        {
                            incubator.Status = IncubatorStatus.AVAILABLE;
                            incubator.UpdatedAt = now;
                            incubator.UpdatedBy = actor;
                            await _incubatorRepository.Update(incubator);
                        }
                    }

                    item.Status = OrderItemStatus.CANCELLED;
                    item.UpdatedAt = now;
                    item.UpdatedBy = actor;
                    await _salesOrderItemRepository.Update(item);
                }

                order.Status = OrderStatus.CANCELLED;
                order.PaymentStatus = order.PaymentStatus == PaymentStatus.PAID ? PaymentStatus.PAID : PaymentStatus.CANCELLED;
                order.UpdatedAt = now;
                order.UpdatedBy = actor;
                await _salesOrderRepository.Update(order);

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.CancelOrderSuccessfully, true);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(e, "Error cancelling order {OrderId}", command.OrderId);
                return ResultModelUtils.FillResult<bool>("500", e.Message, false);
            }
        }

        public async Task<ResultModel<SalesOrderDetailResponse?>> GetById(Guid id, Guid? currentUserId, string role)
        {
            var order = await _salesOrderRepository.FindById(id);
            if (order == null)
                return ResultModelUtils.FillResult<SalesOrderDetailResponse?>("404", CommonConst.OrderNotFound, null);

            if (role == UserRole.CUSTOMER.ToString())
            {
                if (!currentUserId.HasValue)
                    return ResultModelUtils.FillResult<SalesOrderDetailResponse?>("401", CommonConst.Unauthorized, null);

                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null || order.CustomerId != customer.Id)
                    return ResultModelUtils.FillResult<SalesOrderDetailResponse?>("403", CommonConst.AccessDenied, null);
            }

            var items = await _salesOrderItemRepository.FindByOrderId(id);
            return ResultModelUtils.FillResult<SalesOrderDetailResponse?>("200", CommonConst.Success, new SalesOrderDetailResponse
            {
                Order = order,
                Items = items
            });
        }

        public async Task<ResultModel<PagedResult<SalesOrder>>> List(string? status, Guid? customerId, Guid? currentUserId, string role, int page, int pageSize)
        {
            Guid? effectiveCustomerId = customerId;

            if (role == UserRole.CUSTOMER.ToString())
            {
                if (!currentUserId.HasValue)
                    return ResultModelUtils.FillResult<PagedResult<SalesOrder>>("401", CommonConst.Unauthorized, null);

                var customer = await _customerRepository.FindByUserId(currentUserId.Value);
                if (customer == null)
                    return ResultModelUtils.FillResult<PagedResult<SalesOrder>>("404", CommonConst.CustomerNotFound, null);

                effectiveCustomerId = customer.Id;
            }

            var orders = await _salesOrderRepository.FindAll(status, effectiveCustomerId);
            return ResultModelUtils.FillResult("200", CommonConst.Success, PagingUtils.ToPagedResult(orders, page, pageSize));
        }

        private async Task<PreparedOrderResult> PrepareOrderItems(List<OrderItemCommand> items)
        {
            if (items == null || !items.Any())
            {
                return PreparedOrderResult.FromError("400", CommonConst.AtLeastOneItemRequired);
            }

            if (items.Any(x => x.Quantity <= 0))
            {
                return PreparedOrderResult.FromError("400", CommonConst.QuantityMustBeGreaterThanZero);
            }

            var requestedModelIds = items.Select(x => x.IncubatorModelId).ToHashSet().ToList();
            var incubatorModels = await _incubatorModelRepository.FindByIds(requestedModelIds);
            if (requestedModelIds.Count != incubatorModels.Count)
            {
                return PreparedOrderResult.FromError("400", CommonConst.SelectedProductsInvalid);
            }

            if (incubatorModels.Any(x => x.Status != BaseStatus.ACTIVE))
            {
                return PreparedOrderResult.FromError("400", CommonConst.SelectedProductsInactive);
            }

            if (incubatorModels.Any(x => x.UnitPrice <= 0))
            {
                return PreparedOrderResult.FromError("400", CommonConst.UnitPriceMustBeGreaterThanZero);
            }

            var modelsById = incubatorModels.ToDictionary(x => x.Id, x => x);
            var now = DateTime.UtcNow;
            var orderItems = new List<SalesOrderItem>();
            var paymentItems = new List<PaymentItemRequest>();

            foreach (var item in items)
            {
                var model = modelsById[item.IncubatorModelId];
                paymentItems.Add(new PaymentItemRequest
                {
                    Name = model.Name,
                    Quantity = item.Quantity,
                    Price = model.UnitPrice
                });

                for (var index = 0; index < item.Quantity; index++)
                {
                    orderItems.Add(new SalesOrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = Guid.Empty,
                        IncubatorModelId = item.IncubatorModelId,
                        IncubatorId = null,
                        UnitPrice = model.UnitPrice,
                        Status = OrderItemStatus.PENDING_ASSIGNMENT,
                        CreatedAt = now
                    });
                }
            }

            return new PreparedOrderResult
            {
                OrderItems = orderItems,
                PaymentItems = paymentItems,
                TotalAmount = orderItems.Sum(x => x.UnitPrice)
            };
        }

        private static void ApplyPaymentLink(SalesOrder salesOrder, PaymentLinkResult paymentLink, string actor)
        {
            salesOrder.PaymentLinkId = paymentLink.PaymentLinkId;
            salesOrder.QrCode = paymentLink.QrCode;
            salesOrder.PaymentLinkCreatedAt = DateTime.UtcNow;
            salesOrder.PaymentLinkExpiredAt = paymentLink.ExpiredAt;
            salesOrder.UpdatedAt = DateTime.UtcNow;
            salesOrder.UpdatedBy = actor;
        }

        private static CreateOrderResponse ToCreateOrderResponse(SalesOrder order)
        {
            return new CreateOrderResponse
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                TotalAmount = order.TotalAmount,
                PaymentStatus = order.PaymentStatus,
                PaymentOrderCode = order.PaymentOrderCode,
                PaymentLinkId = order.PaymentLinkId,
                QrCode = order.QrCode,
                PaymentLinkExpiredAt = order.PaymentLinkExpiredAt
            };
        }

        private static string BuildPaymentDescription(string? orderCode)
        {
            var desc = $"Thanh toan {orderCode ?? CommonConst.SalesOrderCodePrefix}";
            return desc.Length > 25 ? desc[..25] : desc;
        }

        private static string GenerateOrderCode()
        {
            return $"{CommonConst.SalesOrderCodePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{CodeGenUtils.GenerateNumeric(4)}";
        }

        private static long GeneratePaymentOrderCode()
        {
            return long.Parse($"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{CodeGenUtils.GenerateNumeric(6)}");
        }

        private sealed class PreparedOrderResult
        {
            public string StatusCode { get; set; } = "200";
            public string Message { get; set; } = CommonConst.Success;
            public List<SalesOrderItem> OrderItems { get; set; } = [];
            public List<PaymentItemRequest> PaymentItems { get; set; } = [];
            public long TotalAmount { get; set; }
            public PreparedOrderError? Error { get; set; }

            public static PreparedOrderResult FromError(string statusCode, string message)
            {
                return new PreparedOrderResult
                {
                    StatusCode = statusCode,
                    Message = message,
                    Error = new PreparedOrderError
                    {
                        StatusCode = statusCode,
                        Message = message
                    }
                };
            }
        }

        private sealed class PreparedOrderError
        {
            public string StatusCode { get; set; } = "400";
            public string Message { get; set; } = CommonConst.InvalidStatus;
        }
    }
}
