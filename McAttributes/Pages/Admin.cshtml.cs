using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace McAttributes.Pages
{
    public class AdminModel : PageModel
    {
        private readonly ILogger<AdminModel> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public AdminModel(ILogger<AdminModel> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
        }

        public string? Message { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostFreezeAllAsync([FromForm] int seconds = 30)
        {
            _logger.LogWarning($"ADMIN: Freezing all requests for {seconds} seconds");
            InfrastructureTestHelper.FreezeAll(seconds);
            Message = $"Application frozen for {seconds} seconds (including health checks)";
            return Page();
        }

        public async Task<IActionResult> OnPostFreezeHealthAsync([FromForm] int seconds = 30)
        {
            _logger.LogWarning($"ADMIN: Freezing health endpoints for {seconds} seconds");
            InfrastructureTestHelper.FreezeHealth(seconds);
            Message = $"Health endpoints frozen for {seconds} seconds";
            return Page();
        }

        public IActionResult OnPostCrashApp()
        {
            _logger.LogCritical("ADMIN: Crashing application via Environment.Exit");
            Task.Run(() =>
            {
                Thread.Sleep(100);
                Environment.Exit(1);
            });
            Message = "Application will crash in 100ms";
            return Page();
        }

        public IActionResult OnPostGracefulShutdown()
        {
            _logger.LogWarning("ADMIN: Initiating graceful shutdown");
            Task.Run(() =>
            {
                Thread.Sleep(500);
                _lifetime.StopApplication();
            });
            Message = "Graceful shutdown initiated";
            return Page();
        }

        public IActionResult OnPostMemoryLeak([FromForm] int megabytes = 100)
        {
            _logger.LogWarning($"ADMIN: Allocating {megabytes}MB of memory");
            InfrastructureTestHelper.AllocateMemory(megabytes);
            Message = $"Allocated {megabytes}MB of memory (not released)";
            return Page();
        }

        public IActionResult OnPostCpuBurn([FromForm] int seconds = 10)
        {
            _logger.LogWarning($"ADMIN: Starting CPU burn for {seconds} seconds");
            Task.Run(() => InfrastructureTestHelper.BurnCpu(seconds));
            Message = $"CPU burn started for {seconds} seconds";
            return Page();
        }

        public IActionResult OnPostSlowResponse([FromForm] int milliseconds = 5000)
        {
            _logger.LogWarning($"ADMIN: Setting slow response delay to {milliseconds}ms");
            InfrastructureTestHelper.SetSlowResponse(milliseconds);
            Message = $"All requests will be delayed by {milliseconds}ms";
            return Page();
        }

        public IActionResult OnPostClearSlowResponse()
        {
            _logger.LogInformation("ADMIN: Clearing slow response delay");
            InfrastructureTestHelper.ClearSlowResponse();
            Message = "Slow response delay cleared";
            return Page();
        }

        public IActionResult OnPostThrowException()
        {
            _logger.LogError("ADMIN: Throwing unhandled exception");
            throw new InvalidOperationException("Admin triggered exception for infrastructure testing");
        }
    }

    public static class InfrastructureTestHelper
    {
        private static DateTime? _freezeAllUntil;
        private static DateTime? _freezeHealthUntil;
        private static int _slowResponseMs;
        private static readonly List<byte[]> _memoryLeaks = new();

        public static bool IsFrozenAll => _freezeAllUntil.HasValue && DateTime.UtcNow < _freezeAllUntil.Value;
        public static bool IsFrozenHealth => _freezeHealthUntil.HasValue && DateTime.UtcNow < _freezeHealthUntil.Value;
        public static int SlowResponseMs => _slowResponseMs;

        public static void FreezeAll(int seconds)
        {
            _freezeAllUntil = DateTime.UtcNow.AddSeconds(seconds);
        }

        public static void FreezeHealth(int seconds)
        {
            _freezeHealthUntil = DateTime.UtcNow.AddSeconds(seconds);
        }

        public static void SetSlowResponse(int milliseconds)
        {
            _slowResponseMs = milliseconds;
        }

        public static void ClearSlowResponse()
        {
            _slowResponseMs = 0;
        }

        public static void AllocateMemory(int megabytes)
        {
            for (int i = 0; i < megabytes; i++)
            {
                _memoryLeaks.Add(new byte[1024 * 1024]); // 1MB
            }
        }

        public static void BurnCpu(int seconds)
        {
            var stopTime = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < stopTime)
            {
                // Busy loop to consume CPU
                for (int i = 0; i < 1000000; i++)
                {
                    _ = Math.Sqrt(i);
                }
            }
        }

        public static async Task ApplyDelayIfNeeded()
        {
            if (_slowResponseMs > 0)
            {
                await Task.Delay(_slowResponseMs);
            }
        }
    }
}

