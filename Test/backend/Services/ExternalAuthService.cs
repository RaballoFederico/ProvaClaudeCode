using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FilmAPI.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExternalAuthService> _logger;

    private static readonly ConcurrentDictionary<string, string> DisplayNames = new(new Dictionary<string, string>
    {
        ["google"] = "Google",
        ["github"] = "GitHub",
        ["microsoft"] = "Microsoft"
    });

    public ExternalAuthService(
        IHttpClientFactory httpClientFactory,
        IAuthService authService,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<ExternalAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public IReadOnlyList<ExternalAuthProviderDTO> GetEnabledProviders()
    {
        var providers = new List<ExternalAuthProviderDTO>();
        foreach (var provider in DisplayNames.Keys)
        {
            var cfg = GetProviderConfig(provider);
            if (cfg.IsEnabled)
            {
                providers.Add(new ExternalAuthProviderDTO
                {
                    Provider = provider,
                    DisplayName = DisplayNames[provider]
                });
            }
        }

        return providers;
    }

    public (string? redirectUrl, string? error) CreateAuthorizationUrl(string provider, string? returnUrl, string backendBaseUrl)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        if (!DisplayNames.ContainsKey(normalizedProvider))
        {
            return (null, "PROVIDER_NOT_SUPPORTED");
        }

        var cfg = GetProviderConfig(normalizedProvider);
        if (!cfg.IsEnabled)
        {
            return (null, "PROVIDER_NOT_CONFIGURED");
        }

        var finalReturnUrl = ResolveReturnUrl(returnUrl);
        if (string.IsNullOrWhiteSpace(finalReturnUrl))
        {
            return (null, "RETURN_URL_NOT_ALLOWED");
        }

        var nonce = CreateRandomUrlSafeToken();
        _cache.Set(GetStateCacheKey(nonce), true, TimeSpan.FromMinutes(10));

        var state = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new ExternalAuthState
        {
            Provider = normalizedProvider,
            ReturnUrl = finalReturnUrl,
            Nonce = nonce
        }));

        var redirectUri = BuildCallbackUrl(backendBaseUrl, normalizedProvider);
        var authorizationUrl = BuildAuthorizationUrl(normalizedProvider, cfg, redirectUri, state);

        return (authorizationUrl, null);
    }

    public async Task<string> HandleCallbackAsync(string provider, string backendBaseUrl, string? code, string? state, string? oauthError)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var fallbackUrl = ResolveReturnUrl(null) ?? "/login.html";

        if (!DisplayNames.ContainsKey(normalizedProvider))
        {
            return AppendQuery(fallbackUrl, "extAuthError", "Provider non supportato");
        }

        if (!string.IsNullOrWhiteSpace(oauthError))
        {
            return AppendQuery(fallbackUrl, "extAuthError", "Accesso esterno annullato o negato");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return AppendQuery(fallbackUrl, "extAuthError", "Risposta OAuth non valida");
        }

        ExternalAuthState? parsedState;
        try
        {
            parsedState = JsonSerializer.Deserialize<ExternalAuthState>(Base64UrlDecode(state));
        }
        catch
        {
            return AppendQuery(fallbackUrl, "extAuthError", "State OAuth non valido");
        }

        if (parsedState == null ||
            parsedState.Provider != normalizedProvider ||
            string.IsNullOrWhiteSpace(parsedState.ReturnUrl) ||
            string.IsNullOrWhiteSpace(parsedState.Nonce))
        {
            return AppendQuery(fallbackUrl, "extAuthError", "State OAuth non valido");
        }

        var returnUrl = ResolveReturnUrl(parsedState.ReturnUrl);
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return AppendQuery(fallbackUrl, "extAuthError", "Return URL non consentito");
        }

        if (!_cache.TryGetValue(GetStateCacheKey(parsedState.Nonce), out _))
        {
            return AppendQuery(returnUrl, "extAuthError", "Sessione OAuth scaduta");
        }

        _cache.Remove(GetStateCacheKey(parsedState.Nonce));

        try
        {
            var cfg = GetProviderConfig(normalizedProvider);
            var redirectUri = BuildCallbackUrl(backendBaseUrl, normalizedProvider);
            var profile = await ExchangeCodeAndFetchProfileAsync(normalizedProvider, cfg, code, redirectUri);

            var (response, error) = await _authService.LoginOrRegisterExternalAsync(
                normalizedProvider,
                profile.ProviderUserId,
                profile.Email,
                profile.DisplayName,
                profile.SuggestedUsername);

            if (response == null)
            {
                _logger.LogWarning("External login failed for {Provider}: {Error}", normalizedProvider, error);
                return AppendQuery(returnUrl, "extAuthError", "Autenticazione esterna non riuscita");
            }

            var authCode = CreateRandomUrlSafeToken();
            _cache.Set(GetAuthCodeCacheKey(authCode), response, TimeSpan.FromMinutes(2));

            var withCode = AppendQuery(returnUrl, "extAuthCode", authCode);
            return AppendQuery(withCode, "provider", normalizedProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External callback failed for provider {Provider}", normalizedProvider);
            return AppendQuery(returnUrl, "extAuthError", "Errore durante il login con provider esterno");
        }
    }

    public Task<(LoginResponseDTO? response, string? error)> CompleteAsync(ExternalAuthCompleteRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.AuthCode))
        {
            return Task.FromResult<(LoginResponseDTO?, string?)>((null, "INVALID_REQUEST"));
        }

        var normalizedProvider = request.Provider.Trim().ToLowerInvariant();
        if (!DisplayNames.ContainsKey(normalizedProvider))
        {
            return Task.FromResult<(LoginResponseDTO?, string?)>((null, "PROVIDER_NOT_SUPPORTED"));
        }

        if (!_cache.TryGetValue(GetAuthCodeCacheKey(request.AuthCode), out LoginResponseDTO? response) || response == null)
        {
            return Task.FromResult<(LoginResponseDTO?, string?)>((null, "AUTH_CODE_INVALID_OR_EXPIRED"));
        }

        _cache.Remove(GetAuthCodeCacheKey(request.AuthCode));
        return Task.FromResult<(LoginResponseDTO?, string?)>((response, null));
    }

    private async Task<ExternalProfile> ExchangeCodeAndFetchProfileAsync(string provider, ProviderConfig cfg, string code, string redirectUri)
    {
        var client = _httpClientFactory.CreateClient();

        return provider switch
        {
            "google" => await HandleGoogleAsync(client, cfg, code, redirectUri),
            "github" => await HandleGitHubAsync(client, cfg, code, redirectUri),
            "microsoft" => await HandleMicrosoftAsync(client, cfg, code, redirectUri),
            _ => throw new InvalidOperationException("Provider non supportato")
        };
    }

    private async Task<ExternalProfile> HandleGoogleAsync(HttpClient client, ProviderConfig cfg, string code, string redirectUri)
    {
        var tokenPayload = new Dictionary<string, string>
        {
            ["client_id"] = cfg.ClientId,
            ["client_secret"] = cfg.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        };

        using var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(tokenPayload));
        tokenResponse.EnsureSuccessStatusCode();

        using var tokenDoc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Google access token mancante");

        using var req = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var userInfoResponse = await client.SendAsync(req);
        userInfoResponse.EnsureSuccessStatusCode();

        using var userDoc = JsonDocument.Parse(await userInfoResponse.Content.ReadAsStringAsync());
        var root = userDoc.RootElement;

        return new ExternalProfile
        {
            ProviderUserId = root.GetProperty("sub").GetString() ?? throw new InvalidOperationException("Google sub mancante"),
            Email = root.GetProperty("email").GetString() ?? throw new InvalidOperationException("Google email mancante"),
            DisplayName = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null,
            SuggestedUsername = root.TryGetProperty("given_name", out var givenNameEl) ? givenNameEl.GetString() : null
        };
    }

    private async Task<ExternalProfile> HandleGitHubAsync(HttpClient client, ProviderConfig cfg, string code, string redirectUri)
    {
        var tokenPayload = new Dictionary<string, string>
        {
            ["client_id"] = cfg.ClientId,
            ["client_secret"] = cfg.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        };

        using var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(tokenPayload)
        };
        tokenReq.Headers.Accept.ParseAdd("application/json");

        using var tokenResponse = await client.SendAsync(tokenReq);
        tokenResponse.EnsureSuccessStatusCode();

        using var tokenDoc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("GitHub access token mancante");

        using var userReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        userReq.Headers.UserAgent.ParseAdd("FilmAPI-ExternalAuth");
        userReq.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var userResponse = await client.SendAsync(userReq);
        userResponse.EnsureSuccessStatusCode();

        using var userDoc = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
        var root = userDoc.RootElement;

        var providerUserId = root.GetProperty("id").GetInt64().ToString();
        var login = root.TryGetProperty("login", out var loginEl) ? loginEl.GetString() : null;
        var displayName = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : login;
        var email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;

        if (string.IsNullOrWhiteSpace(email))
        {
            using var emailReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            emailReq.Headers.UserAgent.ParseAdd("FilmAPI-ExternalAuth");
            emailReq.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var emailResponse = await client.SendAsync(emailReq);
            emailResponse.EnsureSuccessStatusCode();

            using var emailDoc = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync());
            foreach (var item in emailDoc.RootElement.EnumerateArray())
            {
                var isPrimary = item.TryGetProperty("primary", out var primaryEl) && primaryEl.GetBoolean();
                var isVerified = item.TryGetProperty("verified", out var verifiedEl) && verifiedEl.GetBoolean();
                if (isPrimary && isVerified)
                {
                    email = item.GetProperty("email").GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                var first = emailDoc.RootElement.EnumerateArray().FirstOrDefault();
                if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("email", out var firstEmailEl))
                {
                    email = firstEmailEl.GetString();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("GitHub email mancante");
        }

        return new ExternalProfile
        {
            ProviderUserId = providerUserId,
            Email = email,
            DisplayName = displayName,
            SuggestedUsername = login
        };
    }

    private async Task<ExternalProfile> HandleMicrosoftAsync(HttpClient client, ProviderConfig cfg, string code, string redirectUri)
    {
        var tenantId = string.IsNullOrWhiteSpace(cfg.TenantId) ? "common" : cfg.TenantId;
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var tokenPayload = new Dictionary<string, string>
        {
            ["client_id"] = cfg.ClientId,
            ["client_secret"] = cfg.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        };

        using var tokenResponse = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenPayload));
        tokenResponse.EnsureSuccessStatusCode();

        using var tokenDoc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Microsoft access token mancante");

        using var req = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/oidc/userinfo");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var userResponse = await client.SendAsync(req);
        userResponse.EnsureSuccessStatusCode();

        using var userDoc = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
        var root = userDoc.RootElement;

        var providerUserId = root.TryGetProperty("sub", out var subEl)
            ? subEl.GetString()
            : root.TryGetProperty("oid", out var oidEl)
                ? oidEl.GetString()
                : null;

        var email = root.TryGetProperty("email", out var emailEl)
            ? emailEl.GetString()
            : root.TryGetProperty("preferred_username", out var preferredEl)
                ? preferredEl.GetString()
                : null;

        if (string.IsNullOrWhiteSpace(providerUserId) || string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Microsoft claims insufficienti");
        }

        return new ExternalProfile
        {
            ProviderUserId = providerUserId,
            Email = email,
            DisplayName = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null,
            SuggestedUsername = root.TryGetProperty("given_name", out var givenNameEl) ? givenNameEl.GetString() : null
        };
    }

    private string BuildAuthorizationUrl(string provider, ProviderConfig cfg, string redirectUri, string state)
    {
        return provider switch
        {
            "google" => $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={Uri.EscapeDataString(cfg.ClientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString("openid profile email")}&state={Uri.EscapeDataString(state)}&prompt=select_account",
            "github" => $"https://github.com/login/oauth/authorize?client_id={Uri.EscapeDataString(cfg.ClientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString("read:user user:email")}&state={Uri.EscapeDataString(state)}",
            "microsoft" => $"https://login.microsoftonline.com/{Uri.EscapeDataString(string.IsNullOrWhiteSpace(cfg.TenantId) ? "common" : cfg.TenantId)}/oauth2/v2.0/authorize?response_type=code&client_id={Uri.EscapeDataString(cfg.ClientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString("openid profile email")}&state={Uri.EscapeDataString(state)}&prompt=select_account",
            _ => throw new InvalidOperationException("Provider non supportato")
        };
    }

    private ProviderConfig GetProviderConfig(string provider)
    {
        var section = _configuration.GetSection($"ExternalAuth:Providers:{provider}");
        var upperProvider = provider.ToUpperInvariant();
        var clientId = Environment.GetEnvironmentVariable($"EXTERNAL_AUTH_{upperProvider}_CLIENT_ID")
            ?? section["ClientId"]
            ?? string.Empty;
        var clientSecret = Environment.GetEnvironmentVariable($"EXTERNAL_AUTH_{upperProvider}_CLIENT_SECRET")
            ?? section["ClientSecret"]
            ?? string.Empty;
        var tenantId = Environment.GetEnvironmentVariable($"EXTERNAL_AUTH_{upperProvider}_TENANT_ID")
            ?? section["TenantId"];

        return new ProviderConfig
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            TenantId = tenantId,
            IsEnabled = !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret)
        };
    }

    private string? ResolveReturnUrl(string? returnUrl)
    {
        var defaultReturnUrl = _configuration["ExternalAuth:DefaultReturnUrl"];
        var candidate = string.IsNullOrWhiteSpace(returnUrl) ? defaultReturnUrl : returnUrl;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var allowed = _configuration.GetSection("ExternalAuth:AllowedReturnUrls").Get<string[]>() ?? Array.Empty<string>();
        if (allowed.Length == 0)
        {
            return candidate;
        }

        foreach (var allowedPrefix in allowed)
        {
            if (!string.IsNullOrWhiteSpace(allowedPrefix) && candidate.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }

    private static string BuildCallbackUrl(string backendBaseUrl, string provider)
    {
        return $"{backendBaseUrl.TrimEnd('/')}/auth/external/{provider}/callback";
    }

    private static string CreateRandomUrlSafeToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes.ToArray());
    }

    private static string GetStateCacheKey(string nonce) => $"ext-auth-state:{nonce}";
    private static string GetAuthCodeCacheKey(string authCode) => $"ext-auth-code:{authCode}";

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }

    private sealed class ProviderConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public bool IsEnabled { get; set; }
    }

    private sealed class ExternalAuthState
    {
        public string Provider { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
    }

    private sealed class ExternalProfile
    {
        public string ProviderUserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? SuggestedUsername { get; set; }
    }
}
