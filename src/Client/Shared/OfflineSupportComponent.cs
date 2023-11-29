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

    /// <summary>
    /// <para>
    /// An <see cref="Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider"/>
    /// instance.
    /// </para>
    /// <para>
    /// May be <see langword="null"/>.
    /// </para>
    /// </summary>
    protected AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    /// <summary>
    /// An <see cref="System.Net.Http.HttpClient"/> instance.
    /// </summary>
    protected HttpClient? HttpClient { get; set; }

    /// <summary>
    /// An injected <see cref="Microsoft.AspNetCore.Components.NavigationManager"/> instance.
    /// </summary>
    [Inject, NotNull] protected NavigationManager? NavigationManager { get; set; }

    /// <summary>
    /// An injected <see cref="IServiceProvider"/> instance.
    /// </summary>
    [Inject, NotNull] protected IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// An injected <see cref="Tavenem.Blazor.Framework.SnackbarService"/> instance.
    /// </summary>
    [Inject, NotNull] protected SnackbarService? SnackbarService { get; set; }

    /// <summary>
    /// An injected <see cref="Client.WikiBlazorClientOptions"/> instance.
    /// </summary>
    [Inject, NotNull] protected WikiBlazorClientOptions? WikiBlazorClientOptions { get; set; }

    /// <summary>
    /// An injected <see cref="Blazor.WikiDataManager"/> instance.
    /// </summary>
    [Inject, NotNull] protected WikiDataManager? WikiDataManager { get; set; }

    /// <summary>
    /// An injected <see cref="Tavenem.Wiki.WikiOptions"/> instance.
    /// </summary>
    [Inject, NotNull] protected WikiOptions? WikiOptions { get; set; }

    /// <summary>
    /// An injected <see cref="Client.WikiState"/> instance.
    /// </summary>
    [Inject, NotNull] protected WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        AuthenticationStateProvider = ServiceProvider.GetService<AuthenticationStateProvider>();
        HttpClient = ServiceProvider.GetService<HttpClient>();
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

    /// <summary>
    /// Fetches data from the wiki server, or the offline store.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve.</typeparam>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="type">THe JSON type info</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>The result.</returns>
    protected async Task<T?> FetchDataAsync<T>(
        string url,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<T?>> fetchLocal)
    {
        T? result = default;

        var isLocal = string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var fetchedFromServer = false;
        if (!isLocal && HttpClient is not null)
        {
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
            try
            {
                result = await fetchLocal.Invoke(user);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return default;
            }
            catch (Exception ex)
            {
                WikiState.LoadError = true;
                SnackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return default;
            }
        }

        return result;
    }

    /// <summary>
    /// Fetches an integer from the wiki server, or the offline store.
    /// </summary>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>The result.</returns>
    protected async Task<int> FetchIntAsync(
        string url,
        Func<ClaimsPrincipal?, Task<int>> fetchLocal)
    {
        var isLocal = string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        if (!isLocal && HttpClient is not null)
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
                return await fetchLocal.Invoke(user);
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

    /// <summary>
    /// Fetches a string from the wiki server, or the offline store.
    /// </summary>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>The result.</returns>
    protected async Task<string?> FetchStringAsync(
        string url,
        Func<ClaimsPrincipal?, Task<string?>> fetchLocal)
    {
        var isLocal = string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        if (!isLocal && HttpClient is not null)
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
                return await fetchLocal.Invoke(user);
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

    /// <summary>
    /// POSTs data to the wiki server, or the offline store.
    /// </summary>
    /// <typeparam name="TSend">The type of data to send.</typeparam>
    /// <typeparam name="TReturn">The type of data which will be returned.</typeparam>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="value">The data to send.</param>
    /// <param name="postedType">JSON type info for the data sent.</param>
    /// <param name="returnType">JSON type info for the data to be returned.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>The result.</returns>
    protected async Task<TReturn?> PostAsync<TSend, TReturn>(
        string url,
        TSend value,
        JsonTypeInfo<TSend> postedType,
        JsonTypeInfo<TReturn> returnType,
        Func<ClaimsPrincipal?, Task<TReturn?>> fetchLocal)
    {
        TReturn? result = default;

        var isLocal = string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
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
        if (!isLocal && HttpClient is not null)
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
                    result = await fetchLocal.Invoke(user);
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

    /// <summary>
    /// POSTs data to the wiki server, or the offline store.
    /// </summary>
    /// <typeparam name="T">The type of data to send.</typeparam>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="value">The data to send.</param>
    /// <param name="type">JSON type info for the data sent.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <param name="failMessage">
    /// A message to supply when the operation fails, and no message is returned.
    /// </param>
    /// <returns>
    /// A <see cref="FetchResult"/> with information about the success of the operation.
    /// </returns>
    protected async Task<FetchResult> PostAsync<T>(
        string url,
        T value,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<bool>> fetchLocal,
        string? failMessage = null)
    {
        var isLocal = string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        if (!isLocal && HttpClient is not null)
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
                var success = await fetchLocal.Invoke(user);
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

    /// <summary>
    /// POSTs data to the wiki server, or the offline store.
    /// </summary>
    /// <typeparam name="T">The type of data to send.</typeparam>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="value">The data to send.</param>
    /// <param name="type">JSON type info for the data sent.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>A string.</returns>
    protected async Task<string?> PostForStringAsync<T>(
        string url,
        T value,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<string?>> fetchLocal)
    {
        string? result = null;

        var isLocal = string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(WikiState.WikiDomain)
            && WikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await WikiBlazorClientOptions.IsOfflineDomain.Invoke(WikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        if (AuthenticationStateProvider is not null)
        {
            state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var fetchedFromServer = false;
        if (!isLocal && HttpClient is not null)
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
                result = await fetchLocal.Invoke(user);
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
