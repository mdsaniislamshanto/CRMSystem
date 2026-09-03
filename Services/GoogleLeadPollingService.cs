using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;

namespace CRMSystem.Services
{
    public class GoogleLeadPollingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GoogleLeadPollingService> _logger;

        // Check Google Form every 1 minute
        private static readonly TimeSpan PollingInterval =
            TimeSpan.FromMinutes(1);

        public GoogleLeadPollingService(
            IServiceScopeFactory scopeFactory,
            ILogger<GoogleLeadPollingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Google Lead Polling Service started.");

            // -----------------------------------------------------
            // Run once immediately when application starts
            // -----------------------------------------------------

            await ImportNewLeadsAsync(stoppingToken);

            // -----------------------------------------------------
            // Continue polling until application stops
            // -----------------------------------------------------

            using var timer =
                new PeriodicTimer(PollingInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(
                    stoppingToken))
                {
                    await ImportNewLeadsAsync(
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "Google Lead Polling Service is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error occurred in Google Lead Polling Service.");
            }
        }

        private async Task ImportNewLeadsAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var googleFormsService =
                    scope.ServiceProvider
                        .GetRequiredService<IGoogleFormsService>();

                var leadService =
                    scope.ServiceProvider
                        .GetRequiredService<ILeadService>();

                _logger.LogInformation(
                    "Checking Google Forms for new leads...");

                var candidates =
                    await googleFormsService
                        .GetLeadCandidatesAsync();

                var newCandidates =
                    candidates
                        .Where(x =>
                            !x.IsAlreadyImported)
                        .ToList();

                if (!newCandidates.Any())
                {
                    _logger.LogInformation(
                        "No new Google Form leads found.");

                    return;
                }

                _logger.LogInformation(
                    "Found {Count} new Google Form lead(s).",
                    newCandidates.Count);

                var importedCount = 0;
                var failedCount = 0;

                foreach (var candidate in newCandidates)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        await leadService
                            .CreateLeadFromCaptureAsync(
                                candidate);

                        importedCount++;

                        _logger.LogInformation(
                            "Google Form lead imported successfully. " +
                            "ReferenceId: {ReferenceId}",
                            candidate.SourceReferenceId);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;

                        _logger.LogError(
                            ex,
                            "Failed to import Google Form lead. " +
                            "ReferenceId: {ReferenceId}",
                            candidate.SourceReferenceId);
                    }
                }

                _logger.LogInformation(
                    "Google Form import completed. " +
                    "Imported: {ImportedCount}, Failed: {FailedCount}",
                    importedCount,
                    failedCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "Google Form import operation was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Google Form automatic import failed.");
            }
        }
    }
}