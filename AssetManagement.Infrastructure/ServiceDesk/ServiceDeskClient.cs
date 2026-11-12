using System.Net.Http.Headers;
using System.Text.Json;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Users;
using Microsoft.Extensions.Options;

namespace AssetManagement.Infrastructure.ServiceDesk;

public sealed class ServiceDeskClient : IServiceDeskClient
{
    private const string RequestsEndpoint = "api/v3/requests";
    private const string AcceptMediaType = "application/vnd.manageengine.sdp.v3+json";
    private const string CategoriesEndpoint = "api/v3/categories";
    private const string AssetCategoryName = "Eszközök";
    private const int LookupPageSize = 100;
    private readonly HttpClient _httpClient;
    private readonly ServiceDeskOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public ServiceDeskClient(HttpClient httpClient, IOptions<ServiceDeskOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options.Value;

        ValidateConfiguration();

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(AcceptMediaType));
    }

    /// <summary>
    /// Creates a new request in ServiceDesk Plus and returns
    /// the external request identifier.
    /// </summary>
    /// <param name="subject">
    /// The subject of the ServiceDesk request.
    /// </param>
    /// <param name="description">
    /// The description of the ServiceDesk request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the HTTP operation.
    /// </param>
    /// <returns>
    /// The identifier of the newly created ServiceDesk request.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the subject is empty.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when the ServiceDesk API request fails.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the ServiceDesk API returns an invalid response.
    /// </exception>
    public async Task<string> CreateRequestAsync(
        string subject,
        ApplicationUser requester,
        string description,
        AssetType assetType,
        CancellationToken cancellationToken = default)
    {
        var creationSendInvoked = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateRequest(subject);

            ArgumentNullException.ThrowIfNull(requester);

            if (string.IsNullOrWhiteSpace(requester.DisplayName))
            {
                throw new ArgumentException("The ServiceDesk requester name cannot be empty.", nameof(requester));
            }

            var categoryId = await ResolveCategoryIdAsync(AssetCategoryName, cancellationToken);

            var subcategoryId = await ResolveSubcategoryIdAsync(categoryId, assetType.ToString(), cancellationToken);

            var payload = BuildCreateRequestPayload(
                subject,
                requester,
                description,
                categoryId,
                subcategoryId);

            var json = JsonSerializer.Serialize(payload, JsonOptions);

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["input_data"] = json
                });

            using var request = new HttpRequestMessage(HttpMethod.Post, RequestsEndpoint);

            request.Headers.TryAddWithoutValidation("authtoken", _options.ApiKey);

            request.Content = content;

            cancellationToken.ThrowIfCancellationRequested();

            // From this point onward, the external creation result
            // must be treated as potentially uncertain.
            creationSendInvoked = true;

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync( cancellationToken);

                throw new HttpRequestException(
                    $"ServiceDesk Plus request creation failed. " +
                    $"HTTP {(int)response.StatusCode} " +
                    $"({response.ReasonPhrase}). " +
                    $"Response: {errorBody}",
                    null,
                    response.StatusCode);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            ValidateApiResponse(document.RootElement);

            return ExtractRequestId(document.RootElement);
        }
        catch (Exception exception) when (!creationSendInvoked)
        {
            throw new ServiceDeskRequestNotSentException("ServiceDesk request creation failed before the creation POST was invoked.", exception);
        }
    }

    private static object BuildCreateRequestPayload(
        string subject,
        ApplicationUser user,
        string description,
        string categoryId,
        string subcategoryId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("The ServiceDesk request subject cannot be empty.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(user.DisplayName))
        {
            throw new ArgumentException("The ServiceDesk requester name cannot be empty.", nameof(user));
        }

        if (string.IsNullOrWhiteSpace(categoryId))
        {
            throw new ArgumentException("The ServiceDesk category identifier cannot be empty.", nameof(categoryId));
        }

        if (string.IsNullOrWhiteSpace(subcategoryId))
        {
            throw new ArgumentException("The ServiceDesk subcategory identifier cannot be empty.", nameof(subcategoryId));
        }

        return new
        {
            request = new
            {
                subject = subject.Trim(),
                description = description ?? string.Empty,

                requester = new
                {
                    name = user.DisplayName.Trim()
                },

                category = new
                {
                    id = categoryId
                },

                subcategory = new
                {
                    id = subcategoryId
                }
            }
        };
    }

    /// <summary>
    /// Closes an existing ServiceDesk Plus request.
    /// </summary>
    /// <remarks>
    /// The request status is updated through the ServiceDesk Plus API.
    ///
    /// This method does not retry failed or uncertain HTTP operations.
    /// </remarks>
    /// <param name="serviceDeskRequestId">
    /// The identifier of the external ServiceDesk request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the HTTP operation.
    /// </param>
    public async Task CloseRequestAsync(string serviceDeskRequestId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceDeskRequestId))
        {
            throw new ArgumentException("The ServiceDesk request identifier cannot be empty.", nameof(serviceDeskRequestId));
        }

        var normalizedRequestId = serviceDeskRequestId.Trim();

        if (!long.TryParse(normalizedRequestId, out var parsedRequestId) || parsedRequestId <= 0)
        {
            throw new ArgumentException("The ServiceDesk request identifier must be a positive numeric value.", nameof(serviceDeskRequestId));
        }

        var payload = new
        {
            request = new
            {
                status = new
                {
                    name = "Lezárt"
                },

            },
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["input_data"] = json
            });

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{RequestsEndpoint}/{parsedRequestId}");

        request.Headers.TryAddWithoutValidation("authtoken", _options.ApiKey);

        request.Content = content;

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"ServiceDesk Plus request closure failed. " +
                $"HTTP {(int)response.StatusCode} " +
                $"({response.ReasonPhrase}). " +
                $"Response: {errorBody}",
                null,
                response.StatusCode);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        ValidateApiResponse(document.RootElement);
    }

    private static void ValidateApiResponse(JsonElement root)
    {
        if (!root.TryGetProperty("response_status", out var responseStatus))
        {
            return;
        }

        if (responseStatus.ValueKind == JsonValueKind.Array)
        {
            foreach (var statusElement in responseStatus.EnumerateArray())
            {
                ValidateResponseStatusElement(statusElement);
            }

            return;
        }

        if (responseStatus.ValueKind == JsonValueKind.Object)
        {
            ValidateResponseStatusElement(responseStatus);
        }
    }
    private static string ExtractRequestId(JsonElement root)
    {
        if (!root.TryGetProperty("request", out var requestElement))
        {
            throw new InvalidOperationException("ServiceDesk Plus response does not contain a request object.");
        }

        if (!requestElement.TryGetProperty("id", out var idElement))
        {
            throw new InvalidOperationException("ServiceDesk Plus response does not contain a request identifier.");
        }

        var requestId = idElement.ValueKind switch
        {
            JsonValueKind.String => idElement.GetString(),
            JsonValueKind.Number => idElement.GetRawText(),

            _ => null
        };

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new InvalidOperationException("ServiceDesk Plus returned an empty request identifier.");
        }

        return requestId;
    }

    private static void ValidateResponseStatusElement(JsonElement statusElement)
    {
        if (!statusElement.TryGetProperty("status", out var statusProperty))
        {
            return;
        }

        var status = statusProperty.GetString();

        if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var message = ExtractErrorMessage(statusElement);

        throw new InvalidOperationException($"ServiceDesk Plus API request failed: {message}");
    }

    private static string ExtractErrorMessage(JsonElement statusElement)
    {
        if (statusElement.TryGetProperty("messages", out var messagesElement))
        {
            if (messagesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var messageElement in messagesElement.EnumerateArray())
                {
                    if (messageElement.TryGetProperty("message", out var messageProperty))
                    {
                        return messageProperty.GetString() ?? "Unknown ServiceDesk Plus API error.";
                    }
                }
            }
        }

        if (statusElement.TryGetProperty("message", out var directMessage))
        {
            return directMessage.GetString() ?? "Unknown ServiceDesk Plus API error.";
        }

        return "Unknown ServiceDesk Plus API error.";
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("ServiceDesk Plus BaseUrl is not configured.");
        }

        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("ServiceDesk Plus BaseUrl is invalid.");
        }

        if (baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("ServiceDesk Plus BaseUrl must use HTTPS.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("ServiceDesk Plus API key is not configured.");
        }
    }

    private static void ValidateRequest(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("ServiceDesk request subject cannot be empty.", nameof(subject));
        }

        if (subject.Length > 250)
        {
            throw new ArgumentException("ServiceDesk request subject cannot exceed 250 characters.", nameof(subject));
        }
    }
    private async Task<string> ResolveCategoryIdAsync(string categoryName, CancellationToken cancellationToken)
    {
        var startIndex = 1;

        while (true)
        {
            using var document = await GetLookupPageAsync(CategoriesEndpoint, startIndex, cancellationToken);

            var root = document.RootElement;

            if (!root.TryGetProperty("categories", out var categories) || categories.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"ServiceDesk Plus returned an invalid category list. Response: {root.GetRawText()}");
            }

            foreach (var category in categories.EnumerateArray())
            {
                if (IsDeleted(category))
                {
                    continue;
                }

                var name = GetStringProperty(category, "name");

                if (!string.Equals(name, categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var id = GetStringProperty(category, "id");

                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException("The matching ServiceDesk category has no identifier.");
                }

                return id;
            }

            if (categories.GetArrayLength() < LookupPageSize)
            {
                break;
            }

            startIndex += LookupPageSize;
        }

        throw new InvalidOperationException($"ServiceDesk category '{categoryName}' was not found.");
    }

    private async Task<string> ResolveSubcategoryIdAsync(string categoryId, string subcategoryName, CancellationToken cancellationToken)
    {
        var startIndex = 0;

        while (true)
        {
            var endpoint = $"{CategoriesEndpoint}/{Uri.EscapeDataString(categoryId)}/subcategories";

            using var document = await GetLookupPageAsync(endpoint, startIndex, cancellationToken);

            var root = document.RootElement;

            if (!root.TryGetProperty("subcategories", out var subcategories) || subcategories.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("ServiceDesk Plus returned an invalid subcategory list.");
            }

            foreach (var subcategory in subcategories.EnumerateArray())
            {
                if (IsDeleted(subcategory))
                {
                    continue;
                }

                var name = GetStringProperty(subcategory, "name");

                if (!string.Equals(name, subcategoryName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var id = GetStringProperty(subcategory, "id");

                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException("The matching ServiceDesk subcategory has no identifier.");
                }

                return id;
            }

            if (subcategories.GetArrayLength() < LookupPageSize)
            {
                break;
            }

            startIndex += LookupPageSize;
        }

        throw new InvalidOperationException($"ServiceDesk subcategory '{subcategoryName}' was not found under category '{categoryId}'.");
    }

    private async Task<JsonDocument> GetLookupPageAsync(string endpoint, int startIndex, CancellationToken cancellationToken)
    {
        var inputData = new
        {
            list_info = new
            {
                row_count = LookupPageSize,
                start_index = startIndex,
                sort_field = "id",
                sort_order = "asc",
                get_total_count = true
            }
        };

        var json = JsonSerializer.Serialize(inputData);

        var requestUrl = $"{endpoint}?input_data={Uri.EscapeDataString(json)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        request.Headers.TryAddWithoutValidation("authtoken", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"ServiceDesk lookup failed. " +
                $"HTTP {(int)response.StatusCode}. " +
                $"Response: {errorBody}",
                null,
                response.StatusCode);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        try
        {
            ValidateApiResponse(document.RootElement);
            return document;
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }
    private static bool IsDeleted(JsonElement element)
        {
            return element.TryGetProperty("deleted", out var deleted) && deleted.ValueKind == JsonValueKind.True;
        }

        private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    /// <summary>
    /// Creates a private ServiceDesk Plus request note.
    /// </summary>
    /// <remarks>
    /// The note is used exclusively for service case event logging.
    /// No task or worklog is created.
    /// </remarks>
    public async Task<string> CreateRequestNoteAsync(
        string serviceDeskRequestId,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceDeskRequestId))
        {
            throw new ArgumentException("The ServiceDesk request identifier cannot be empty.", nameof(serviceDeskRequestId));
        }

        if (!long.TryParse(serviceDeskRequestId.Trim(), out var requestId) || requestId <= 0)
        {
            throw new ArgumentException("The ServiceDesk request identifier must be a positive numeric value.", nameof(serviceDeskRequestId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("The ServiceDesk note description cannot be empty.", nameof(description));
        }

        var payload = new
        {
            note = new
            {
                description = description.Trim(),
                show_to_requester = false,
                notify_technician = false,
                mark_first_response = false,
                add_to_linked_requests = false
            }
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["input_data"] = json
            });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{RequestsEndpoint}/{requestId}/notes");

        request.Headers.TryAddWithoutValidation("authtoken", _options.ApiKey);

        request.Content = content;

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"ServiceDesk Plus request note creation failed. " +
                $"HTTP {(int)response.StatusCode} " +
                $"({response.ReasonPhrase}). " +
                $"Response: {errorBody}",
                null,
                response.StatusCode);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        ValidateApiResponse(document.RootElement);

        return ExtractRequestNoteId(document.RootElement);
    }

    /// <summary>
    /// Extracts the identifier of a newly created ServiceDesk request note.
    /// </summary>
    private static string ExtractRequestNoteId(JsonElement root)
    {
        if (!root.TryGetProperty("note", out var noteElement) || noteElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("ServiceDesk Plus response does not contain a note object.");
        }

        if (!noteElement.TryGetProperty("id", out var idElement))
        {
            throw new InvalidOperationException("ServiceDesk Plus response does not contain a note identifier.");
        }

        var noteId = idElement.ValueKind switch
        {
            JsonValueKind.String => idElement.GetString(),
            JsonValueKind.Number => idElement.GetRawText(),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(noteId))
        {
            throw new InvalidOperationException("ServiceDesk Plus returned an empty note identifier.");
        }

        return noteId;
    }
}