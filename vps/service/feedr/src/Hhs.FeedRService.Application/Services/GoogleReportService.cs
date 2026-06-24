using System.Text;
using Google.Ads.AdManager.V1;
using Hhs.FeedRService.Application.Contracts.DashboardDomain;
using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos.Reporting;
using Hhs.FeedRService.Domain.ConfigurationDomain.Repositories.MongoDB;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DateTime = System.DateTime;
using IGoogleReportService = Hhs.FeedRService.Application.Contracts.JobDomain.IGoogleReportService;

namespace Hhs.FeedRService.Application.Services;

public class ReportService: ApplicationServiceBase, IGoogleReportService
{
    private readonly IFrameworkLogger _logger;
    private readonly ICustomerConfigurationRepository _customerConfigurationRepository;
    private readonly INetworkConfigurationRepository _networkConfigurationRepository;
    private readonly IReportPersistenceService _persistenceService;
    private readonly IConfiguration _configuration;

    public ReportService(
        IServiceProvider provider,
        ICustomerConfigurationRepository customerConfigurationRepository,
        INetworkConfigurationRepository networkConfigurationRepository,
        IReportPersistenceService persistenceService,
        IConfiguration configuration
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();
        _customerConfigurationRepository = customerConfigurationRepository;
        _networkConfigurationRepository = networkConfigurationRepository;
        _persistenceService = persistenceService;
        _configuration = configuration;
    }

    public CreateReportRequest CreateReportRequest(CreateReportRequestDto input)
    {
        ReportDefinition.Types.Dimension dimension = string.IsNullOrWhiteSpace(input.AdUnitIdTopLevel)
            ? ReportDefinition.Types.Dimension.AdUnitId
            : ReportDefinition.Types.Dimension.AdUnitIdTopLevel;

        var createReportRequest = new CreateReportRequest
        {
            Parent = input.Parent,
            Report = new Report()
            {
                DisplayName = input.DisplayName,
                #region ReportDefinition
                ReportDefinition = new ReportDefinition
                {
                    Dimensions = { dimension, ReportDefinition.Types.Dimension.AdUnitId, ReportDefinition.Types.Dimension.DemandChannel,ReportDefinition.Types.Dimension.DemandSubchannelName, ReportDefinition.Types.Dimension.OrderId, ReportDefinition.Types.Dimension.OrderName, ReportDefinition.Types.Dimension.Date},
                    Metrics =
                    {
                        ReportDefinition.Types.Metric.CodeServedCount,ReportDefinition.Types.Metric.Impressions, ReportDefinition.Types.Metric.AverageEcpm, ReportDefinition.Types.Metric.Revenue,ReportDefinition.Types.Metric.ActiveViewEligibleImpressions
                    },
                    DateRange = new ReportDefinition.Types.DateRange
                    {
                        Relative = input.DateRange
                    },
                    ReportType = ReportDefinition.Types.ReportType.Historical,
                    Filters =
                    {
                        new ReportDefinition.Types.Filter
                        {
                            FieldFilter = new ReportDefinition.Types.Filter.Types.FieldFilter
                            {
                                Field = new ReportDefinition.Types.Field
                                {
                                    Dimension = dimension,
                                },
                                MetricValueType = ReportDefinition.Types.MetricValueType.Primary,
                                Operation = ReportDefinition.Types.Filter.Types.Operation.Matches,
                                Values =
                                {
                                    new ReportValue()
                                    {
                                        StringValue = input.AdUnitIdTopLevel
                                    }
                                }
                            }
                        }
                    }
                    /*,
                    Sorts =
                    {
                        new ReportDefinition.Types.Sort
                        {
                                Field = new ReportDefinition.Types.Field
                                {
                                    Dimension = ReportDefinition.Types.Dimension.AdUnitName,
                                },
                                MetricValueType = ReportDefinition.Types.MetricValueType.Primary,
                                Descending = false
                        }
                    }*/
                }

                #endregion ReportDefinition
            }
        };
        return createReportRequest;
    }

    public Task<GenerateSummaryReportRes> RetrieveInventories(Guid appClientId)
    {
        throw new NotImplementedException();
    }

    public async Task<GenerateSummaryReportRes> GenerateSummaryReportAsync(Guid appClientId, string dateRange)
    {
        try
        {
            if(appClientId == Guid.Empty)
            {
                _logger.LogWarning("AppClientId is empty. Generating summary report for all clients.");
                return await GenerateSummaryReportForAllAsync(dateRange);
            }
            
            var clientSettings = await _customerConfigurationRepository.FindByUniqueKeysAsync(appClientId);
            if (clientSettings is null) throw new ArgumentNullException(nameof(appClientId));
            if(dateRange is null) dateRange = "YESTERDAY";

            var reportDate = DateTime.Today; // Google Ad Manager "Yesterday" relative range
            var requestId = $"summary-{clientSettings.ClientId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            foreach (string adUnitId in clientSettings.AdUnitId)
            {
                var createReportRequest = CreateReportRequest(new CreateReportRequestDto
                {
                    Parent = $"networks/{clientSettings.Network}",
                    DisplayName = $"Summary_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{clientSettings.ClientName}",
                    AdUnit = adUnitId,
                    DateRange = (ReportDefinition.Types.DateRange.Types.RelativeDateRange)Enum.Parse(typeof(ReportDefinition.Types.DateRange.Types.RelativeDateRange), dateRange, true)
                });
                _logger.LogDebug($"Running report for AdUnitId={adUnitId}, ClientId={clientSettings.ClientId}");

                string rawResponse = await CreateReportAsync(createReportRequest);
                if (string.IsNullOrWhiteSpace(rawResponse))
                {
                    _logger.LogWarning($"Empty Google Ad Manager response for AdUnitId={adUnitId}");
                    continue;
                }

                _logger.LogDebug(dateRange);
                _logger.LogDebug(rawResponse);
                
                // Stage 1: persist raw response to MongoDB.
                var rawId = await _persistenceService.PersistRawResponseAsync(
                    tenantId: clientSettings.TenantId,
                    requestId: $"{requestId}-{adUnitId}",
                    jobName: "GenerateSummaryReport",
                    network: clientSettings.Network,
                    adUnitIdTopLevel: string.Empty,
                    adUnitId: adUnitId,
                    reportDate: reportDate,
                    rawResponseBody: rawResponse);

                // Stage 2: derive to PostgreSQL normalized per-client, per-day records.
                await _persistenceService.DeriveToPostgresAsync(rawId);
                
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"GenerateSummaryReportAsync failed: {e.Message}");
            throw;
        }
        return new GenerateSummaryReportRes();
    }

    public async Task<GenerateSummaryReportRes> GenerateSummaryReportForAllAsync(string dateRange)
    {
        var networks = await _networkConfigurationRepository.GetAllActiveAsync();
        if (networks == null || networks.Count == 0)
        {
            _logger.LogWarning("No active NetworkConfiguration documents found. Falling back to legacy single-client path.");
            return await GenerateSummaryReportAsync(Guid.Empty, dateRange);
        }
        
        if(dateRange is null) dateRange = "YESTERDAY";
        foreach (var network in networks)
        {
            foreach (var topLevelGroup in network.TopLevelGroups.Where(g => g.IsActive))
            {
                try
                {
                    await GenerateReportForTopLevelGroupAsync(
                        network.TenantId,
                        network.NetworkCode,
                        network.DisplayName,
                        topLevelGroup.AdUnitTopLevelCode,
                        dateRange);
                }
                catch (Exception e)
                {
                    _logger.LogError($"GenerateSummaryReportForAllAsync failed for Network={network.NetworkCode}, TopLevel={topLevelGroup.AdUnitTopLevelCode}: {e.Message}");
                }
            }
        }

        return new GenerateSummaryReportRes();
    }

    /// <summary>
    /// Generates a single Google Ad Manager report for an entire TopLevelGroup.
    /// The response contains rows for all AdUnitIds (clients) under that group.
    /// Client-level breakdown happens during the PostgreSQL derivation phase.
    /// </summary>
    private async Task GenerateReportForTopLevelGroupAsync(
        Guid tenantId,
        string networkCode,
        string networkDisplayName,
        string adUnitIdTopLevel,
        string dateRange)
    {
        var reportDate = DateTime.Today;
        var requestId = $"summary-{networkDisplayName}-{adUnitIdTopLevel}-dateRange-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var createReportRequest = CreateReportRequest(new CreateReportRequestDto
        {
            Parent = $"networks/{networkCode}",
            DisplayName = $"Summary_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{networkDisplayName}_{adUnitIdTopLevel}",
            AdUnit = string.Empty,
            AdUnitIdTopLevel = adUnitIdTopLevel,
            DateRange = (Google.Ads.AdManager.V1.ReportDefinition.Types.DateRange.Types.RelativeDateRange)Enum.Parse(
                typeof(Google.Ads.AdManager.V1.ReportDefinition.Types.DateRange.Types.RelativeDateRange), dateRange, true)
        });

        _logger.LogDebug($"Running report for Network={networkDisplayName}, TopLevel={adUnitIdTopLevel}");

        string rawResponse = await CreateReportAsync(createReportRequest);
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            _logger.LogWarning($"Empty Google Ad Manager response for Network={networkDisplayName}, TopLevel={adUnitIdTopLevel}");
            return;
        }

        // Persist raw response to MongoDB as a single document for the entire TopLevelGroup.
        // AdUnitId is left empty — individual client AdUnitIds are inside the parsed rows.
        var rawId = await _persistenceService.PersistRawResponseAsync(
            tenantId: tenantId,
            requestId: requestId,
            jobName: "GenerateSummaryReport",
            network: networkCode,
            adUnitIdTopLevel: adUnitIdTopLevel,
            adUnitId: string.Empty,
            reportDate: reportDate,
            rawResponseBody: rawResponse);

        // Derive to PostgreSQL — this groups rows by AdUnitId and creates per-client daily records.
        await _persistenceService.DeriveToPostgresAsync(rawId);
    }

    public async Task<string> CreateReportAsync(CreateReportRequest request)
    {
        // Mock mode: read from local file instead of calling Google Ad Manager.
        // Enable by setting GoogleAdManager:MockMode=true and optionally GoogleAdManager:MockResponsePath.
        var mockMode = _configuration.GetValue<bool>("GoogleAdManager:MockMode");
        if (mockMode)
        {
            var mockPath = _configuration.GetValue<string>("GoogleAdManager:MockResponsePath")
                           ?? Path.Combine(AppContext.BaseDirectory, ".vscode", "ExampleGoogleAdmResponse.json");

            if (!File.Exists(mockPath))
            {
                // Fallback: walk up to find the file alongside the solution .vscode folder.
                var probe = new DirectoryInfo(AppContext.BaseDirectory);
                while (probe != null && !File.Exists(Path.Combine(probe.FullName, ".vscode", "ExampleGoogleAdmResponse.json")))
                {
                    probe = probe.Parent;
                }
                if (probe != null)
                {
                    mockPath = Path.Combine(probe.FullName, ".vscode", "ExampleGoogleAdmResponse.json");
                }
            }

            if (!File.Exists(mockPath))
            {
                _logger.LogError($"Mock response file not found. Looked at: {mockPath}");
                return null;
            }

            _logger.LogDebug($"[MOCK] Reading Google Ad Manager response from {mockPath}");
            return await File.ReadAllTextAsync(mockPath);
        }

        try
        {
            string reportResult = null;
            StringBuilder stringBuilder = new StringBuilder();
            var reportServiceClient = await ReportServiceClient.CreateAsync();
            var reportResponse = await reportServiceClient.CreateReportAsync(request);
            var runReportResponse = await reportServiceClient.RunReportAsync(reportResponse.ReportName);
            var pollUntilCompleted = await runReportResponse.PollUntilCompletedAsync();
            if (pollUntilCompleted.IsCompleted)
            {
                reportResult = pollUntilCompleted.Result.ReportResult;
            }
            var response = reportServiceClient.FetchReportResultRowsAsync(reportResult);
            await foreach (var item in response)
            {
                stringBuilder.AppendLine(item.ToString());
            }
            return stringBuilder.ToString();
        }
        catch(Exception ex)
        {
            _logger.LogError($"Error in CreateReportAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<string> RetrieveInventoriesAsync(Guid appClientId)
    {
        try{
            var clientSettings = await _customerConfigurationRepository.FindByUniqueKeysAsync(appClientId);
            if (clientSettings is null) throw new ArgumentNullException(nameof(appClientId));

            StringBuilder stringBuilder = new StringBuilder();
            var orderServiceClient = await OrderServiceClient.CreateAsync();
            var reportResponse =  orderServiceClient.ListOrdersAsync(new ListOrdersRequest(){
                Parent = $"networks/{clientSettings.Network}",
                //Filter = $"ORDER_AD_UNIT_ID_TOP_LEVEL=\"{clientSettings.AdUnitIdTopLevel}\""
                });
                
            await foreach (var item in reportResponse)
            {
                stringBuilder.AppendLine($"{item.DisplayName}, {item.OrderId}, {item.OrderName}");
            }
            Console.WriteLine(stringBuilder.ToString());
            return stringBuilder.ToString();
        }
        catch(Exception ex)
        {
            _logger.LogError($"Error in RetrieveInventoriesAsync: {ex.Message}");
            return null;
        }
    }
}