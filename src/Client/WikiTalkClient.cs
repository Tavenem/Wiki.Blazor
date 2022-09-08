using Microsoft.AspNetCore.SignalR.Client;

namespace Tavenem.Wiki.Blazor.SignalR;

/// <summary>
/// A client for receiving wiki discussion messages.
/// </summary>
public class WikiTalkClient : IWikiTalkClient, IAsyncDisposable
{
    private static readonly TimeSpan _ConnectionTimeout = TimeSpan.FromSeconds(30);

    private readonly HubConnection _hubConnection;

    private CancellationTokenSource? _cts;
    private bool _disposed;
    private string? _topicId;

    /// <summary>
    /// Whether the connection is active.
    /// </summary>
    public bool IsConnected => !_disposed && _hubConnection.State == HubConnectionState.Connected;

    /// <summary>
    /// The current connection state.
    /// </summary>
    public HubConnectionState State => _disposed
        ? HubConnectionState.Disconnected
        : _hubConnection.State;

    /// <summary>
    /// Receive a new message.
    /// </summary>
    public event EventHandler<MessageResponse>? OnRecevied;

    /// <summary>
    /// Initializes a new instance of <see cref="WikiTalkClient"/>.
    /// </summary>
    /// <param name="url">The URL of the hub.</param>
    /// <param name="token">The access token.</param>
    public WikiTalkClient(
        string url,
        string? token)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(url,
                options => options.AccessTokenProvider = () => Task.FromResult(token))
            .WithAutomaticReconnect()
            .Build();
        _hubConnection.On<MessageResponse>(nameof(Receive), Receive);
        _hubConnection.Reconnected += Reconnected;
    }

    /// <summary>
    /// <para>
    /// Stops and disposes the current connection.
    /// </para>
    /// <para>
    /// Does nothing if no connection is active.
    /// </para>
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        _hubConnection.Reconnected -= Reconnected;

        if (_hubConnection.State == HubConnectionState.Connected && !string.IsNullOrEmpty(_topicId))
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource((int)_ConnectionTimeout.TotalMilliseconds);
            try
            {
                await _hubConnection
                    .InvokeAsync(nameof(IWikiTalkHub.LeaveTopic), _topicId, _cts.Token)
                    .ConfigureAwait(false);
            }
            catch { }
        }

        _cts?.Dispose();
        await _hubConnection.DisposeAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// <para>
    /// Receive a new message.
    /// </para>
    /// <para>
    /// Note: this method should only be invoked internally by an <see cref="IWikiTalkHub"/>.
    /// </para>
    /// </summary>
    /// <param name="message">
    /// A <see cref="MessageResponse"/> with information about the message received.
    /// </param>
    public Task Receive(MessageResponse message)
    {
        OnRecevied?.Invoke(this, message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Send a reply.
    /// </summary>
    /// <param name="reply">
    /// <para>
    /// The message that has been sent.
    /// </para>
    /// <para>
    /// Note: messages with empty content are neither saved to the data source, nor forwarded to
    /// clients. Messages with missing topic IDs are also ignored.
    /// </para>
    /// </param>
    public async Task SendAsync(ReplyRequest reply)
    {
        if (_disposed)
        {
            throw new Exception("Client has been disposed.");
        }
        await _hubConnection.SendAsync(nameof(IWikiTalkHub.Send), reply).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>
    /// Starts a connection to the given topic. Re-tries once per second if necessary.
    /// </para>
    /// <para>
    /// Times out after 30 seconds.
    /// </para>
    /// </summary>
    /// <param name="topicId">The ID of the topic to join.</param>
    public async Task StartAsync(string topicId)
    {
        if (_disposed)
        {
            throw new Exception("Client has been disposed.");
        }

        // Retry until server is ready, or timeout.
        _cts?.Dispose();
        _cts = new CancellationTokenSource((int)_ConnectionTimeout.TotalMilliseconds);
        if (_hubConnection.State == HubConnectionState.Connected && !string.IsNullOrEmpty(_topicId))
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await _hubConnection
                        .InvokeAsync(nameof(IWikiTalkHub.LeaveTopic), _topicId, _cts.Token)
                        .ConfigureAwait(false);
                    break;
                }
                catch
                {
                    await Task.Delay(1000, _cts.Token).ConfigureAwait(false);
                }
            }
        }
        while (!_cts.IsCancellationRequested && _hubConnection.State != HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.StartAsync(_cts.Token).ConfigureAwait(false);
                break;
            }
            catch
            {
                await Task.Delay(1000, _cts.Token).ConfigureAwait(false);
            }
        }
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await _hubConnection
                        .InvokeAsync(nameof(IWikiTalkHub.JoinTopic), topicId, _cts.Token)
                        .ConfigureAwait(false);
                    _topicId = topicId;
                    break;
                }
                catch
                {
                    await Task.Delay(1000, _cts.Token).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task Reconnected(string? arg)
    {
        if (_hubConnection.State == HubConnectionState.Connected && !string.IsNullOrEmpty(_topicId))
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource((int)_ConnectionTimeout.TotalMilliseconds);
            await _hubConnection
                .InvokeAsync(nameof(IWikiTalkHub.LeaveTopic), _topicId, _cts.Token)
                .ConfigureAwait(false);
        }
    }
}
