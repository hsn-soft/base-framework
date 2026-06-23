using Microsoft.Extensions.Hosting;

namespace Hhs.Shared.Hosting.Extensions;

public static class EnvironmentExtensions
{
    extension(IHostEnvironment hostEnvironment)
    {
        public bool IsHostProduction()
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment("stage") || hostEnvironment.IsEnvironment("uat")|| hostEnvironment.IsEnvironment("production");
        }

        public bool IsIntegrationTest()
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment(EnvironmentNames.IntegrationTestEnvironment);
        }
    }
}

public static class EnvironmentNames
{
    public const string IntegrationTestEnvironment = nameof(IntegrationTestEnvironment);
}