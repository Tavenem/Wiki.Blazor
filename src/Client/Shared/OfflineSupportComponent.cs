using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Internal.Models;
using Tavenem.Wiki.Blazor.Exceptions;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// A base component for the wiki components which supports fetching either offline or server
/// content.
/// </summary>
public class OfflineSupportComponent : ComponentBase, IDisposable
{
    private bool _disposedValue;

    private protected AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    [Inject] private protected HttpClient HttpClient { get; set; } = default!;

    [Inject] private protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject] private protected IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject] private protected SnackbarService SnackbarService { get; set; } = default!;

    [Inject] private protected WikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private protected WikiDataManager WikiDataManager { get; set; } = default!;

    [Inject] private protected WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private protected WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        AuthenticationStateProvider = ServiceProvider.GetService<AuthenticationStateProvider>();
        if (AuthenticationStateProvider is not null)
        {
            AuthenticationStateProvider.AuthenticationStateChanged += OnStateChanged;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing && AuthenticationStateProvider is not null)
            {
                AuthenticationStateProvider.AuthenticationStateChanged -= OnStateChanged;
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Loads component data.
    /// </summary>
    protected virtual Task RefreshAsync() => Task.CompletedTask;

    [RequiresUnreferencedCode(
        "Calls System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(JsonSerializerOptions, CancellationToken)")]
    private protected async Task<T?> FetchDataAsync<T>(
        string url,
        Func<ClaimsPrincipal?, Task<T?>> fetchOffline)
    {
        T? result = default;

        var isOfflineDomain = false;
        if (!string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isOfflineDomain = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var triedServer = false;
        var fetchedFromServer = false;
        if (!isOfflineDomain
            && !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            triedServer = true;
            try
            {
                var response = await HttpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    SnackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    result = await response.Content.ReadFromJsonAsync<T>();
                    fetchedFromServer = true;
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
                return default;
            }
        }

        if (!fetchedFromServer)
        {
            if (WikiBlazorClientOptions.DataStore is not null)
            {
                try
                {
                    result = await fetchOffline.Invoke(user);
                }
                catch (WikiUnauthorizedException)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                catch (InvalidOperationException ex)
                {
                    SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                catch (ArgumentException ex)
                {
                    SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
            }
            else if (triedServer)
            {
                WikiState.LoadError = true;
            }
        }

        return result;
    }

    private protected async Task<T?> FetchDataAsync<T>(
        string url,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<T?>> fetchOffline)
    {
        T? result = default;

        var isOfflineDomain = false;
        if (!string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isOfflineDomain = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var triedServer = false;
        var fetchedFromServer = false;
        if (!isOfflineDomain
            && !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            triedServer = true;
            try
            {
                var response = await HttpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    SnackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    result = await response.Content.ReadFromJsonAsync(type);
                    fetchedFromServer = true;
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
                return default;
            }
        }

        if (!fetchedFromServer)
        {
            if (WikiBlazorClientOptions.DataStore is not null)
            {
                try
                {
                    result = await fetchOffline.Invoke(user);
                }
                catch (WikiUnauthorizedException)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                catch (InvalidOperationException ex)
                {
                    SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                catch (ArgumentException ex)
                {
                    SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
            }
            else if (triedServer)
            {
                WikiState.LoadError = true;
            }
        }

        return result;
    }

    private protected async Task<int> FetchIntAsync(
        string url,
        Func<ClaimsPrincipal?, Task<int>> fetchOffline)
    {
        var isOfflineDomain = false;
        if (!string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isOfflineDomain = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        if (!isOfflineDomain
            && !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            try
            {
                var response = await HttpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return 0;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    SnackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return 0;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(result, out var value))
                    {
                        return value;
                    }
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
                return 0;
            }
        }

        if (WikiBlazorClientOptions.DataStore is not null)
        {
            try
            {
                return await fetchOffline.Invoke(user);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return 0;
            }
            catch (ArgumentException ex)
            {
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return 0;
            }
        }

        return 0;
    }

    private protected async Task<string?> FetchStringAsync(
        string url,
        Func<ClaimsPrincipal?, Task<string?>> fetchOffline)
    {
        var isOfflineDomain = false;
        if (!string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isOfflineDomain = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        if (!isOfflineDomain
            && !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            try
            {
                var response = await HttpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    SnackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
                return null;
            }
        }

        if (WikiBlazorClientOptions.DataStore is not null)
        {
            try
            {
                return await fetchOffline.Invoke(user);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return null;
            }
            catch (InvalidOperationException ex)
            {
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return null;
            }
            catch (ArgumentException ex)
            {
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return null;
            }
        }

        return null;
    }

    private protected async Task<TReturn?> PostAsync<TSend, TReturn>(
        string url,
        TSend value,
        JsonTypeInfo<TSend> postedType,
        JsonTypeInfo<TReturn> returnType,
        Func<ClaimsPrincipal?, Task<TReturn?>> fetchOffline)
    {
        TReturn? result = default;

        var isOfflineDomain = false;
        if (!string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isOfflineDomain = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var triedServer = false;
        var fetchedFromServer = false;
        if (!isOfflineDomain
            && !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            triedServer = true;
            try
            {
                var response = await HttpClient.PostAsJsonAsync(url, value, postedType);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    SnackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    result = await response.Content.ReadFromJsonAsync(returnType);
                    fetchedFromServer = true;
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
                return default;
            }
        }

        if (!fetchedFromServer)
        {
            if (WikiBlazorClientOptions.DataStore is not null)
            {
                try
                {
                    result = await fetchOffline.Invoke(user);
                }
                catch (WikiUnauthorizedException)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                catch (InvalidOperationException ex)
                {
                    SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                catch (ArgumentException ex)
                {
                    SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
            }
            else if (triedServer)
            {
                WikiState.LoadError = true;
            }
        }

        return result;
    }

    private protected async Task<FetchResult> PostAsync<T>(
        string url,
        T value,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<bool>> fetchOffline,
        string? failMessage = null)
    {
        var isOfflineDomain = false;
        if (!string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isOfflineDomain = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        if (!isOfflineDomain
            && !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync(url, value, type);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return new FetchResult(false);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    SnackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return new FetchResult(false);
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    return new FetchResult(true, response.ReasonPhrase);
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
                return new FetchResult(false);
            }
        }

        if (WikiBlazorClientOptions.DataStore is not null)
        {
            try
            {
                var success = await fetchOffline.Invoke(user);
                return new FetchResult(true, success ? null : failMessage);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return new FetchResult(false);
            }
            catch (InvalidOperationException ex)
            {
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return new FetchResult(false);
            }
            catch (ArgumentException ex)
            {
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return new FetchResult(false);
            }
        }

        return new FetchResult(false);
    }

    private protected async Task<string?> PostForStringAsync<T>(
        string url,
        T value,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<string?>> fetchOffline)
    {
        string? result = null;

        var isOfflineDomain = false;
        if (!string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isOfflineDomain = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var fetchedFromServer = false;
        if (!isOfflineDomain
            && !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync(url, value, type);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    SnackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    result = await response.Content.ReadAsStringAsync();
                    fetchedFromServer = true;
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
                return default;
            }
        }

        if (!fetchedFromServer
            && WikiBlazorClientOptions.DataStore is not null)
        {
            try
            {
                result = await fetchOffline.Invoke(user);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return default;
            }
            catch (InvalidOperationException ex)
            {
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return default;
            }
            catch (ArgumentException ex)
            {
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return default;
            }
        }

        return result;
    }

    private void HandleUnauthorized(AuthenticationState? state)
    {
        if (state?.User.Identity?.IsAuthenticated != true)
        {
            if (state is null
                || string.IsNullOrEmpty(WikiBlazorClientOptions.LoginPath))
            {
                NavigationManager.NavigateTo(
                    NavigationManager.GetUriWithQueryParameter(
                        nameof(Wiki.Unauthenticated),
                        true));
            }
            else
            {
                var path = new StringBuilder(WikiBlazorClientOptions.LoginPath)
                    .Append(WikiBlazorClientOptions.LoginPath.Contains('?')
                        ? '&' : '?')
                    .Append("returnUrl=")
                    .Append(UrlEncoder.Default.Encode(NavigationManager.Uri));
                NavigationManager.NavigateTo(path.ToString());
            }
        }
        WikiState.NotAuthorized = true;
    }

    private async void OnStateChanged(object? sender) => await RefreshAsync();
}
