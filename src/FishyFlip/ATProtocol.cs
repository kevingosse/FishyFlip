// <copyright file="ATProtocol.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

namespace FishyFlip;

/// <summary>
/// AT Protocol.
/// https://atproto.com/specs/atp.
/// </summary>
public sealed class ATProtocol : IDisposable
{
    private ATProtocolOptions options;
    private HttpClient client;
    private bool disposedValue;
    private ISessionManager sessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ATProtocol"/> class.
    /// </summary>
    /// <param name="options">Configuration options for ATProto. <see cref="ATProtocolOptions"/>.</param>
    public ATProtocol(ATProtocolOptions options)
    {
        this.options = options;
        this.client = options.HttpClient ?? throw new NullReferenceException(nameof(options.HttpClient));
        if (options.Session is not null)
        {
            this.sessionManager = new PasswordSessionManager(this, options.Session);
        }
        else
        {
            this.sessionManager = new PasswordSessionManager(this);
        }
    }

    /// <summary>
    /// Event for when a session is updated.
    /// </summary>
    public event EventHandler<SessionUpdatedEventArgs>? OnSessionUpdated;

    /// <summary>
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated => this.sessionManager.IsAuthenticated;

    /// <summary>
    /// Gets the ATProtocol Options.
    /// </summary>
    public ATProtocolOptions Options => this.options;

    /// <summary>
    /// Gets the ATProto Server Protocol.
    /// </summary>
    public ATProtoServer Server => new(this);

    /// <summary>
    /// Gets the current ATProto Session. Null if no session is active.
    /// </summary>
    public Session? Session => this.sessionManager?.Session;

    /// <summary>
    /// Gets the ATProto Admin Protocol.
    /// </summary>
    public ATProtoAdmin Admin => new(this);

    /// <summary>
    /// Gets the ATProto Identity Protocol.
    /// </summary>
    public ATProtoIdentity Identity => new(this);

    /// <summary>
    /// Gets the ATProto Sync Protocol.
    /// </summary>
    public ATProtoSync Sync => new(this);

    /// <summary>
    /// Gets the ATProto Repo Protocol.
    /// </summary>
    public ATProtoRepo Repo => new(this);

    /// <summary>
    /// Gets the ATProto Actor Protocol.
    /// </summary>
    public BlueskyActor Actor => new(this);

    /// <summary>
    /// Gets the ATProto Label Protocol.
    /// </summary>
    public ATProtoLabel Label => new(this);

    /// <summary>
    /// Gets the ATProto Moderation Protocol.
    /// </summary>
    public ATProtoModeration Moderation => new(this);

    /// <summary>
    /// Gets the ATProto Unspecced Protocol.
    /// </summary>
    public BlueskyUnspecced Unspecced => new(this);

    /// <summary>
    /// Gets the ATProto Feed Protocol.
    /// </summary>
    public BlueskyFeed Feed => new(this);

    /// <summary>
    /// Gets the ATProto Graph Protocol.
    /// </summary>
    public BlueskyGraph Graph => new(this);

    /// <summary>
    /// Gets the ATProto Notification Protocol.
    /// </summary>
    public BlueskyNotification Notification => new(this);

    /// <summary>
    /// Gets the PclDirectory Methods.
    /// </summary>
    public PlcDirectory PlcDirectory => new(this);

    /// <summary>
    /// Gets the ATProto Chat Protocol.
    /// </summary>
    public BlueskyChat Chat => new(this);

    /// <summary>
    /// Gets the base address for the underlying HttpClient.
    /// </summary>
    public Uri? BaseAddress => this.client.BaseAddress;

    /// <summary>
    /// Gets the HttpClient.
    /// </summary>
    public HttpClient Client => this.client;

    /// <summary>
    /// Gets the internal session manager.
    /// </summary>
    internal ISessionManager SessionManager => this.sessionManager;

    /// <summary>
    /// Update the Instance Uri.
    /// </summary>
    /// <param name="uri">Instance Uri.</param>
    public void UpdateInstanceUri(Uri uri)
    {
        this.options.Url = uri;
        this.UpdateOptions(this.options);
    }

    /// <summary>
    /// Update options for ATProto.
    /// This will log out the current session and dispose of the current session manager.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <exception cref="NullReferenceException">Thrown if missing options.</exception>
    public void UpdateOptions(ATProtocolOptions options)
    {
        this.options = options;
        this.options.UpdateHttpClient(options.Url);
        this.client = options.HttpClient ?? throw new NullReferenceException(nameof(options.HttpClient));
        this.sessionManager.Dispose();
        if (options.Session is not null)
        {
            this.sessionManager = new PasswordSessionManager(this, options.Session);
        }
        else
        {
            this.sessionManager = new PasswordSessionManager(this);
        }
    }

    /// <summary>
    /// Refreshes the current session asynchronously.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// If the session manager is null, the task will complete immediately.
    /// Otherwise, the task will complete when the session has been refreshed.
    /// </returns>
    public Task RefreshSessionAsync()
        => this.sessionManager?.RefreshSessionAsync() ?? Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Run when a user logs in.
    /// </summary>
    /// <param name="session"><see cref="Session"/>.</param>
    internal void OnUserLoggedIn(Session session)
    {
        if (this.options.UseServiceEndpointUponLogin)
        {
            var logger = this.options?.Logger;
            var serviceUrl = session.DidDoc?.Service?.FirstOrDefault()?.ServiceEndpoint;
            if (string.IsNullOrEmpty(serviceUrl))
            {
                logger?.LogWarning($"UseServiceEndpointUponLogin enabled, but session missing Service Endpoint.");
            }
            else
            {
                var result = Uri.TryCreate(serviceUrl, UriKind.Absolute, out Uri? uriResult);
                if (!result || uriResult is null)
                {
                    logger?.LogWarning($"UseServiceEndpointUponLogin enabled, but session missing Service Endpoint.");
                }
                else
                {
                    this.Options.Url = uriResult;
                    this.Options.Session = session;
                    logger?.LogInformation($"UseServiceEndpointUponLogin enabled, switching to {uriResult}.");
                    this.UpdateOptions(this.Options);
                    return;
                }
            }
        }

        if (this.sessionManager is null)
        {
            this.sessionManager = new PasswordSessionManager(this);
        }

        this.SetSession(session);
    }

    /// <summary>
    /// Sets the current session.
    /// </summary>
    /// <param name="session"><see cref="Session"/>.</param>
    internal void SetSession(Session session)
    {
        this.sessionManager?.SetSession(session);
        this.OnSessionUpdated?.Invoke(this, new SessionUpdatedEventArgs(session, this.BaseAddress));
    }

    private void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                this.client.Dispose();
            }

            this.disposedValue = true;
        }
    }
}