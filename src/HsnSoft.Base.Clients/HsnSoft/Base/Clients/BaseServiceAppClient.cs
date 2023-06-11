using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;
using HsnSoft.Base.ExceptionHandling.Dtos;
using IdentityModel.Client;
using JetBrains.Annotations;

namespace HsnSoft.Base.Clients;

public class BaseServiceAppClient
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

    protected async Task CheckResult(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            ErrorDtoResponse err;
            try { err = await response.Content.ReadFromJsonAsync<ErrorDtoResponse>(); }
            catch (Exception) { err = null; }

            throw new Exception(err == null ? response.ReasonPhrase : err.StatusMessagesToSingleMessage());
        }
    }
}