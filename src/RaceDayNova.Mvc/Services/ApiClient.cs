using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace RaceDayNova.Mvc.Services;

/// <summary>Single integration point for the Part 2 API. MVC does not access the database.</summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    public ApiClient(HttpClient http, IHttpContextAccessor accessor) { _http = http; _accessor = accessor; }

    private HttpRequestMessage Create(HttpMethod method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(method, path);
        var cookie = _accessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrWhiteSpace(cookie)) req.Headers.TryAddWithoutValidation("Cookie", cookie);
        if (body != null) req.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        return req;
    }
    private async Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try { response = await _http.SendAsync(request, ct); }
        catch (HttpRequestException)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "API unavailable",
                Content = new StringContent("{\"title\":\"Part 2 API unavailable\",\"detail\":\"Start the RaceDayNova.Api project and check Api:BaseUrl in the MVC configuration.\"}", Encoding.UTF8, "application/problem+json")
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "API timeout",
                Content = new StringContent("{\"title\":\"API request timed out\",\"detail\":\"The API took too long to respond. Please try again.\"}", Encoding.UTF8, "application/problem+json")
            };
        }
        var context = _accessor.HttpContext;
        if (context != null && response.Headers.TryGetValues("Set-Cookie", out var cookies))
            foreach (var value in cookies) context.Response.Headers.Append("Set-Cookie", value);
        return response;
    }
    public async Task<(T? Data, string? Error, HttpStatusCode Status)> Get<T>(string path)
    {
        using var response = await Send(Create(HttpMethod.Get, path)); return await Read<T>(response);
    }
    public async Task<(T? Data, string? Error, HttpStatusCode Status)> Post<T>(string path, object body)
    {
        using var response = await Send(Create(HttpMethod.Post, path, body)); return await Read<T>(response);
    }
    public async Task<(T? Data, string? Error, HttpStatusCode Status)> Put<T>(string path, object body)
    {
        using var response = await Send(Create(HttpMethod.Put, path, body)); return await Read<T>(response);
    }
    public async Task<(bool Ok, string? Error, HttpStatusCode Status)> Delete(string path)
    {
        using var response = await Send(Create(HttpMethod.Delete, path)); var text = await response.Content.ReadAsStringAsync(); return (response.IsSuccessStatusCode, ExtractError(text, response.ReasonPhrase), response.StatusCode);
    }
    public async Task<(string? Url, string? Error, HttpStatusCode Status)> Upload(string path, IFormFile file)
    {
        using var request = Create(HttpMethod.Post, path);
        var multipart = new MultipartFormDataContent(); await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream); fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        multipart.Add(fileContent, "file", Path.GetFileName(file.FileName)); request.Content = multipart;
        using var response = await Send(request); var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return (null, ExtractError(text, response.ReasonPhrase), response.StatusCode);
        try { using var doc = JsonDocument.Parse(text); return (doc.RootElement.TryGetProperty("url", out var url) ? url.GetString() : null, null, response.StatusCode); }
        catch { return (null, "The API returned an unexpected upload response.", response.StatusCode); }
    }
    private static async Task<(T? Data, string? Error, HttpStatusCode Status)> Read<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return (default, ExtractError(text, response.ReasonPhrase), response.StatusCode);
        if (string.IsNullOrWhiteSpace(text)) return (default, null, response.StatusCode);
        try { return (JsonSerializer.Deserialize<T>(text, JsonOptions), null, response.StatusCode); }
        catch (JsonException) { return (default, "The API returned data in an unexpected format.", response.StatusCode); }
    }
    private static string ExtractError(string text, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback ?? "The request could not be completed.";
        try { using var doc = JsonDocument.Parse(text); var root = doc.RootElement; foreach (var key in new[] { "detail", "title", "message", "Message" }) if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString()!; if (root.TryGetProperty("errors", out var errors)) return string.Join(" ", errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString())); }
        catch { } return text.Length > 240 ? text[..240] : text;
    }
}
