using CRMSystem.Services.Interfaces;

namespace CRMSystem.BackgroundServices
{
    public class SLABackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SLABackgroundService> _logger;

        public SLABackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SLABackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SLA Background Service started.");


            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var slaService =
                        scope.ServiceProvider
                            .GetRequiredService<ISLAService>();


                    await slaService
                        .CheckAcceptanceSLAsAsync();


                    await slaService
                        .CheckFirstFeedbackSLAsAsync();


                    await slaService
                        .CheckNextFeedbackSLAsAsync();


                    _logger.LogInformation(
                        "SLA checks completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An error occurred while checking SLA.");
                }


                try
                {
                    await Task.Delay(
                        TimeSpan.FromMinutes(1),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }


            _logger.LogInformation(
                "SLA Background Service stopped.");
        }
    }
}