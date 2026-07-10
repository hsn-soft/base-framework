using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Configuration;

namespace Hhs.Shared.Hosting.Helpers;

public static class ThreadPoolConfigurationHelper
{
    public static void Configure(IConfiguration configuration, IAppConsoleLogger logger)
    {
        ThreadPool.GetMinThreads(out int defaultMinWorker, out int defaultMinIo);
        ThreadPool.GetMaxThreads(out int maxWorker, out int maxIo);

        int minWorker = ParseOrDefault(configuration["Threading:MinimumWorkerThreads"], defaultMinWorker);
        int minIo = ParseOrDefault(configuration["Threading:MinimumCompletionPortThreads"], defaultMinIo);

        ThreadPool.SetMinThreads(minWorker, minIo);
        // Deliberately not touching SetMaxThreads — .NET's default max is already effectively unbounded
        // (core-scaled, tens of thousands); lowering it is the classic footgun (caps real capacity),
        // raising it rarely does anything. Min-thread pre-warming is the only lever that matters here.

        logger.LogInformation(
            "ThreadPool configured: MinWorker={MinWorker} MinIO={MinIO} MaxWorker={MaxWorker} MaxIO={MaxIO} ProcessorCount={ProcessorCount}",
            minWorker, minIo, maxWorker, maxIo, Environment.ProcessorCount);
    }

    private static int ParseOrDefault(string? value, int fallback)
        => int.TryParse(value, out int parsed) && parsed > 0 ? parsed : fallback;
}
