using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TripPin.Model;

namespace TripPin;

public class PeopleService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PeopleService> logger) : IPeopleService
{
    public async Task<List<User>> Search(string? filter)
    {
        try
        {
            var response = await SendRequest(HttpMethod.Get, $"People{(string.IsNullOrEmpty(filter) ? "" : $"?$filter={WebUtility.UrlDecode(filter)}")}");

            var responseMessage = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseMessage);
            if (!responseObject.ContainsKey("value"))
            {
                logger.LogError("Invalid response format: {Response}", responseMessage);
                throw new WebException("Invalid response format!");
            }
            
            
            var users = responseObject["value"]!.ToObject<List<User>>()!;
            return users;
        }
        catch (WebException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception");
            throw;
        }
    }

    public async Task<User> GetByUserName(string userName)
    {
        try
        {
            var response = await SendRequest(HttpMethod.Get, $"People('{userName}')");
        
            var responseMessage = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseMessage);
            if (!responseObject.ContainsKey("UserName"))
            {
                logger.LogError("Invalid response format: {Response}", responseMessage);
                throw new WebException("Invalid response format!");
            }
            
            var user = JsonConvert.DeserializeObject<User>(responseMessage)!;
            return user;
        }
        catch (WebException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception");
            throw;
        }
    }

    public async Task<bool> UpdateUserField(string userName, Dictionary<string, string> fieldsToUpdate)
    {
        var key = await GetServerKey();
        var response = await SendRequest(HttpMethod.Patch, $"{key}/People('{userName}')", fieldsToUpdate);
        
        return true;
    }

    private async Task<string> GetServerKey()
    {
        var client = httpClientFactory.CreateClient();
        var baseUrl = configuration.GetValue<string>("ApiBaseUrl")!;
        var response = await client.GetAsync(baseUrl);
        var responseMessage = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Unable to retrieve server key. Status code: {StatusCode}, message: {Message}", response.StatusCode, responseMessage);
            throw new WebException("Unable to retrieve server key");
        }
        
        var key = JObject.Parse(responseMessage)?.Value<string>("@odata.context")?
            .Replace(baseUrl, "")
            .Split("/")[0];
        
        if (string.IsNullOrEmpty(key))
            throw new WebException("Missing server key");

        return key;
    }

    private async Task<HttpResponseMessage> SendRequest(HttpMethod method, string endpoint, object? body = null)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{configuration.GetValue<string>("ApiBaseUrl")}{endpoint}"),
            Method = method
        };
        
        if (body != null)
            request.Content = new StringContent(JsonConvert.SerializeObject(body),  Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            logger.LogError("Error response received, status code: {StatusCode}, message: {Message}", response.StatusCode, message);
            throw new WebException("Error during request");
        }

        return response;
    }
}