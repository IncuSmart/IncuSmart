namespace IncuSmart.Core.Responses
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class IncubatorResponse
    {
        public Guid Id { get; set; }
        public Guid ModelId { get; set; }
        public string? ModelName { get; set; }
        public string? SerialNumber { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public IncubatorStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    public class HatchingSeasonTemplateDetailResponse
    {
        public HatchingSeasonTemplate? Template { get; set; }
        public List<TemplateBatchDetailResponse> Batches { get; set; } = [];
    }

    public class TemplateBatchDetailResponse
    {
        public HatchingSeasonTemplateBatch? Batch { get; set; }
        public List<HatchingSeasonTemplateBatchConfig> Configs { get; set; } = [];
    }

    public class HatchingBatchDetailResponse
    {
        public HatchingBatch? Batch { get; set; }
        public List<HatchingBatchConfig> Configs { get; set; } = [];
    }

    public class HatchingSeasonDetailResponse
    {
        public HatchingSeason? Season { get; set; }
        public HatchingSeasonTemplate? Template { get; set; }
        public List<HatchingBatchDetailResponse> Batches { get; set; } = [];
    }

    public class SalesOrderDetailResponse
    {
        public SalesOrder? Order { get; set; }
        public List<SalesOrderItem> Items { get; set; } = [];
    }

    public class CreateOrderResponse
    {
        public Guid OrderId { get; set; }
        public string? OrderCode { get; set; }
        public long TotalAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public long? PaymentOrderCode { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? QrCode { get; set; }
        public DateTime? PaymentLinkExpiredAt { get; set; }
    }

    public class MaintenanceTicketDetailResponse
    {
        public MaintenanceTicket? Ticket { get; set; }
        public Warranty? Warranty { get; set; }
        public List<MaintenanceLog> Logs { get; set; } = [];
    }

    public class CustomerSummaryResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string UserStatus { get; set; } = string.Empty;
        public string CustomerStatus { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class CustomerDetailResponse : CustomerSummaryResponse
    {
        public DateTime UserCreatedAt { get; set; }
        public DateTime? UserUpdatedAt { get; set; }
        public DateTime CustomerCreatedAt { get; set; }
        public DateTime? CustomerUpdatedAt { get; set; }
    }
}
