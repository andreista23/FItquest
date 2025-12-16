using FitQuest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace FitQuest.Services
{
    public class EvidenceCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EvidenceCleanupService> _logger;

        public EvidenceCleanupService(
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            ILogger<EvidenceCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOnce(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Evidence cleanup failed.");
                }

                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        private async Task CleanupOnce(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;

            var targets = await db.Evidence
                .Where(e => !string.IsNullOrEmpty(e.FilePath) &&
                            (e.ExpiresAt <= now || e.Validated == true))
                .ToListAsync(ct);

            if (targets.Count == 0)
                return;

            int deletedFiles = 0;

            foreach (var e in targets)
            {
                if (TryDeleteFile(e.FilePath))
                {
                    deletedFiles++;
                }

                e.FilePath = string.Empty;
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Evidence cleanup: {Count} evidences processed, {Deleted} files deleted.",
                targets.Count, deletedFiles);
        }

        private bool TryDeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var relative = filePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var fullPath = Path.Combine(_env.WebRootPath, relative);

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete evidence file: {FullPath}", fullPath);
            }

            return false;
        }
    }
}
