namespace IncuSmart.Infra.Services
{
    public class PayOSWebhookRegistrationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<IncuSmart.Core.Utils.PayOSOptions> _options;
        private readonly ILogger<PayOSWebhookRegistrationHostedService> _logger;

        public PayOSWebhookRegistrationHostedService(
            IServiceProvider serviceProvider,
            IOptions<IncuSmart.Core.Utils.PayOSOptions> options,
            ILogger<PayOSWebhookRegistrationHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.Value.AutoConfirmWebhook)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.Value.ClientId)
                || string.IsNullOrWhiteSpace(_options.Value.ApiKey)
                || string.IsNullOrWhiteSpace(_options.Value.ChecksumKey))
            {
                _logger.LogWarning("Skipping payOS webhook confirmation because configuration is incomplete.");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var paymentGatewayService = scope.ServiceProvider.GetRequiredService<IPaymentGatewayService>();

            try
            {
                await paymentGatewayService.ConfirmWebhook(_options.Value.WebhookUrl);
                _logger.LogInformation("Confirmed payOS webhook URL: {WebhookUrl}", _options.Value.WebhookUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to confirm payOS webhook URL: {WebhookUrl}", _options.Value.WebhookUrl);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
