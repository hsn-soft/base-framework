using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;
using HsnSoft.Base.Communication;
using IdentityModel.Client;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace HsnSoft.Base.Clients;

public abstract class BaseServiceAppClient
{
    private readonly HttpClient _httpClient;

    protected BaseServiceAppClient(IHttpClientFactory httpClientFactory, string clientName = null)
    {
        _httpClient = string.IsNullOrWhiteSpace(clientName) ? httpClientFactory.CreateClient() : httpClientFactory.CreateClient(clientName);
    }

    protected HttpClient GetClient()
    {
        return _httpClient;
    }

    public void SetBearerToken(string token)
    {
        _httpClient.SetBearerToken(token);
    }

    protected string GetQueryString(object obj)
    {
        var properties = from p in obj.GetType().GetProperties()
            where p.GetValue(obj, null) != null
            select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null)?.ToString());

        return string.Join("&", properties.ToArray());
    }

    [ItemCanBeNull]
    protected async Task<T> CheckResultAndReturnModel<T>(HttpResponseMessage response) where T : class
    {
        await CheckResult(response);

        try
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    [ItemCanBeNull]
    protected async Task<string> CheckResultAndReturnString(HttpResponseMessage response)
    {
        await CheckResult(response);

        try
        {
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return null;
        }
    }

    [ItemCanBeNull]
    protected async Task<T> CheckBaseResultAndReturnModel<T>(HttpResponseMessage response) where T : class
    {
        await CheckResult(response);

        try
        {
            var responseContentJson = await response.Content.ReadAsStringAsync();

            var baseModel = JsonConvert.DeserializeObject<BaseResponse<T>>(responseContentJson);

            return baseModel.Payload;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task CheckResult(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        BaseResponse err;
        try { err = await response.Content.ReadFromJsonAsync<BaseResponse>(); }
        catch (Exception) { err = null; }

        err ??= new BaseResponse()
        {
            StatusCode = (int)response.StatusCode,
            StatusMessages = new List<string> { response.ReasonPhrase ?? string.Empty }
        };

        if (err != null)
        {
            throw new Exception($"{err.StatusCode.ToString()}:{err.StatusMessagesToSingleMessage()}");
        }
    }
}