using Hhs.Gateway.Commercial.Options.Docs;
using Hhs.Gateway.Commercial.Services.Swagger;
using JetBrains.Annotations;

namespace Hhs.Gateway.Commercial.Services.Docs;

public static class GatewayDocsBuilder
{
    public static string BuildDocsHtml(
        string gatewayBaseUrl,
        IReadOnlyCollection<GatewayServiceLiveInfo> services,
        IServiceUrlResolver urlResolver)
    {
        var cards = string.Join(Environment.NewLine, services.Select(service => $$$"""
                                                                                   <div class="card">
                                                                                       <div class="title">{{{Html(service.Definition.DisplayName)}}}</div>
                                                                                       <div class="muted">{{{Html(service.Definition.Key)}}}</div>

                                                                                       <div class="item">
                                                                                           <span class="label">Service Swagger UI</span>
                                                                                          <a href="{{{Html(urlResolver.BuildServiceUrl(service.Definition.Key, "/swagger"))}}}" target="_blank">
                                                                                       {{{Html(urlResolver.BuildServiceUrl(service.Definition.Key, "/swagger"))}}}
                                                                                   </a>
                                                                                       </div>

                                                                                       <div class="item">
                                                                                           <span class="label">Gateway Rewritten Swagger JSON</span>
                                                                                           <a href="{{{gatewayBaseUrl}}}/openapi-proxy/{{{Html(service.Definition.Key)}}}" target="_blank">{{{gatewayBaseUrl}}}/openapi-proxy/{{{Html(service.Definition.Key)}}}</a>
                                                                                       </div>

                                                                                       <div class="item">
                                                                                        <span class="label">Gateway Rewritten Swagger JSON</span>
                                                                                        <a href="{{{Html(urlResolver.BuildServiceUrl(service.Definition.Key, "/swagger/v1/swagger.json"))}}}" target="_blank">
                                                                                       {{{Html(urlResolver.BuildServiceUrl(service.Definition.Key, "/swagger/v1/swagger.json"))}}}
                                                                                   </a>
                                                                                    </div>

                                                                                       {{{ProbeBlock("Service Health", service.ServiceHealth)}}}
                                                                                       {{{ProbeBlock("Gateway Health", service.GatewayHealth)}}}
                                                                                       {{{ProbeBlock("Service Root", service.ServiceRoot)}}}
                                                                                       {{{ProbeBlock("Gateway Root", service.GatewayRoot)}}}
                                                                                   </div>
                                                                                   """));

        return $$"""
                 <!DOCTYPE html>
                 <html lang="en">
                 <head>
                     <meta charset="utf-8" />
                     <title>Gateway Docs</title>
                     <style>
                         body { font-family: Arial, sans-serif; background:#0f172a; color:#e2e8f0; margin:0; padding:24px; }
                         h1 { margin-top:0; }
                         .top { margin-bottom:24px; }
                         .top a { color:#93c5fd; text-decoration:none; margin-right:12px; }
                         .grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(380px,1fr)); gap:16px; }
                         .card { background:#111827; border:1px solid #334155; border-radius:14px; padding:16px; }
                         .title { font-size:20px; margin-bottom:8px; }
                         .muted { color:#94a3b8; margin:6px 0; word-break:break-all; }
                         .item { margin:10px 0; }
                         .item a { color:#93c5fd; text-decoration:none; word-break:break-all; }
                         .label { color:#cbd5e1; font-weight:bold; display:block; margin-bottom:4px; }
                         code { color:#f8fafc; background:#0b1220; padding:3px 6px; border-radius:6px; }
                         .probe { margin-top:14px; padding:12px; background:#0b1220; border:1px solid #334155; border-radius:10px; }
                         .probe-title { font-weight:bold; margin-bottom:8px; }
                         .snippet { margin-top:8px; color:#cbd5e1; white-space:pre-wrap; word-break:break-word; }
                         .badge { display:inline-block; margin-left:8px; padding:3px 8px; border-radius:999px; font-size:12px; }
                         .green { background:#14532d; color:#bbf7d0; }
                         .red { background:#7f1d1d; color:#fecaca; }
                         .gray { background:#374151; color:#e5e7eb; }
                     </style>
                 </head>
                 <body>
                     <div class="top">
                         <h1>Gateway Docs Dashboard</h1>
                         <a href="{{gatewayBaseUrl}}/swagger" target="_blank">Gateway Swagger UI</a>
                         <a href="{{gatewayBaseUrl}}/swagger/gateway/swagger.json" target="_blank">Gateway Swagger JSON</a>
                         <a href="{{gatewayBaseUrl}}/gateway-info" target="_blank">Gateway Info</a>
                     </div>
                     <div class="grid">
                         {{cards}}
                     </div>
                 </body>
                 </html>
                 """;
    }

    private static string Html([CanBeNull] string value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    private static string StatusBadge([CanBeNull] EndpointProbeResult probe)
    {
        if (probe is null) return """<span class="badge gray">N/A</span>""";

        return probe.IsSuccess
            ? $"""<span class="badge green">UP · {probe.ResponseTimeMs} ms</span>"""
            : $"""<span class="badge red">DOWN · {probe.ResponseTimeMs} ms</span>""";
    }

    private static string ProbeBlock(string title, [CanBeNull] EndpointProbeResult probe)
    {
        if (probe is null)
        {
            return $$"""
                     <div class="probe">
                         <div class="probe-title">{{title}}</div>
                         <div class="muted">Not configured</div>
                     </div>
                     """;
        }

        return $$"""
                 <div class="probe">
                     <div class="probe-title">{{title}} {{StatusBadge(probe)}}</div>
                     <div><a href="{{Html(probe.Url)}}" target="_blank">{{Html(probe.Url)}}</a></div>
                     <div class="muted">Status: {{Html(probe.StatusCode?.ToString() ?? "-")}}</div>
                     <div class="snippet">{{Html(probe.ResponseSnippet ?? probe.ErrorMessage ?? "")}}</div>
                 </div>
                 """;
    }
}