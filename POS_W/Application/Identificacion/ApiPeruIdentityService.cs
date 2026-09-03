using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace POS_W.Application.Identificacion;

public sealed class ApiPeruIdentityService(HttpClient httpClient, IOptions<ApiPeruOptions> options)
{
    private readonly ApiPeruOptions _options = options.Value;

    public async Task<IdentityLookupResult> LookupRucAsync(string ruc)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            return IdentityLookupResult.Fail("Configura ApiPeru:Token para consultar RUC.");
        }

        if (ruc.Trim().Length != 11 || !ruc.Trim().All(char.IsDigit))
        {
            return IdentityLookupResult.Fail("El RUC debe tener 11 digitos.");
        }

        return await SendAsync("/ruc", new { ruc = ruc.Trim() }, data =>
        {
            var name = ReadString(data, "nombre_o_razon_social");
            if (string.IsNullOrWhiteSpace(name))
            {
                return IdentityLookupResult.Fail("API Peru no devolvio razon social para el RUC.");
            }

            return IdentityLookupResult.Ok(new IdentityLookupData(
                "RUC",
                ReadString(data, "ruc") ?? ruc.Trim(),
                name,
                ReadString(data, "direccion"),
                ReadString(data, "estado"),
                ReadString(data, "condicion")));
        });
    }

    public async Task<IdentityLookupResult> LookupDniAsync(string dni)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            return IdentityLookupResult.Fail("Configura ApiPeru:Token para consultar DNI.");
        }

        if (dni.Trim().Length != 8 || !dni.Trim().All(char.IsDigit))
        {
            return IdentityLookupResult.Fail("El DNI debe tener 8 digitos.");
        }

        return await SendAsync("/dni", new { dni = dni.Trim() }, data =>
        {
            var name = ReadString(data, "nombre_completo");
            if (string.IsNullOrWhiteSpace(name))
            {
                return IdentityLookupResult.Fail("API Peru no devolvio nombre para el DNI.");
            }

            return IdentityLookupResult.Ok(new IdentityLookupData(
                "DNI",
                ReadString(data, "numero") ?? dni.Trim(),
                name,
                null,
                null,
                null));
        });
    }

    private async Task<IdentityLookupResult> SendAsync<TRequest>(
        string path,
        TRequest payload,
        Func<JsonElement, IdentityLookupResult> map)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        try
        {
            using var response = await httpClient.SendAsync(request);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiPeruEnvelope>();
            if (apiResponse is null)
            {
                return IdentityLookupResult.Fail("API Peru devolvio una respuesta vacia.");
            }

            if (!response.IsSuccessStatusCode || !apiResponse.Success || apiResponse.Data is null)
            {
                return IdentityLookupResult.Fail(apiResponse.Message ?? $"Consulta rechazada por API Peru ({(int)response.StatusCode}).");
            }

            return map(apiResponse.Data.Value);
        }
        catch (Exception ex)
        {
            return IdentityLookupResult.Fail($"No se pudo consultar API Peru: {ex.Message}");
        }
    }

    private static string? ReadString(JsonElement data, string property)
    {
        return data.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;
    }
}

public sealed class ApiPeruOptions
{
    public string BaseUrl { get; set; } = "https://api.apiperu.dev";
    public string Token { get; set; } = "";
}

public sealed record IdentityLookupData(
    string DocumentType,
    string DocumentNumber,
    string FullName,
    string? Address,
    string? State,
    string? Condition);

public sealed record IdentityLookupResult(bool Success, string Message, IdentityLookupData? Data)
{
    public static IdentityLookupResult Ok(IdentityLookupData data) => new(true, "Datos encontrados.", data);
    public static IdentityLookupResult Fail(string message) => new(false, message, null);
}

public sealed class ApiPeruEnvelope
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}
