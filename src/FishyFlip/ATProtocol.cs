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
    private bool disposedValue;
    private ISessionManager sessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ATProtocol"/> class.
    /// </summary>
    /// <param name="options">Configuration options for ATProto. <see cref="ATProtocolOptions"/>.</param>
    public ATProtocol(ATProtocolOptions options)
    {
        this.options = options;
        this.sessionManager = new UnauthenticatedSessionManager(options);
    }

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
    public Uri? BaseAddress => this.sessionManager.Client.BaseAddress;

    /// <summary>
    /// Gets the HttpClient.
    /// </summary>
    public HttpClient Client => this.sessionManager.Client;

    /// <summary>
    /// Gets the internal session manager.
    /// </summary>
    internal ISessionManager SessionManager => this.sessionManager;

    /// <summary>
    /// Asynchronously creates a new session manager using a password.
    /// </summary>
    /// <param name="identifier">The identifier of the user.</param>
    /// <param name="password">The password of the user.</param>
    /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the operation.</param>
    /// <returns>A Task that represents the asynchronous operation. The task result contains a Result object with the session details, or null if the session could not be created.</returns>
    public async Task<Session?> AuthenticateWithPasswordAsync(string identifier, string password, CancellationToken cancellationToken = default)
    {
        this.sessionManager.Dispose();
        var passwordSessionManager = new PasswordSessionManager(this);
        this.sessionManager = passwordSessionManager;
        return await passwordSessionManager.CreateSessionAsync(identifier, password, cancellationToken);
    }

    /// <summary>
    /// Starts the OAuth2 authentication process asynchronously.
    /// </summary>
    /// <param name="clientId">ClientID, must be a URL.</param>
    /// <param name="redirectUrl">RedirectUrl.</param>
    /// <param name="scopes">ATProtocol Scopes.</param>
    /// <param name="instanceUrl">InstanceUrl, must be a URL. If null, uses https://bsky.social.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Authorization URL to call.</returns>
    public async Task<string> GenerateOAuth2AuthenticationUrlAsync(string clientId, string redirectUrl, IEnumerable<string> scopes, string? instanceUrl = default, CancellationToken cancellationToken = default)
    {
        this.sessionManager.Dispose();
        var oAuth2SessionManager = new OAuth2SessionManager(this);
        this.sessionManager = oAuth2SessionManager;
        return await oAuth2SessionManager.StartAuthorizationAsync(clientId, redirectUrl, scopes, instanceUrl, cancellationToken);
    }

    /// <summary>
    /// Authenticates with OAuth2 callback asynchronously.
    /// </summary>
    /// <param name="callbackData">The callback data received from the OAuth2 provider.</param>
    /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the operation.</param>
    /// <returns>A Task that represents the asynchronous operation. The task result contains a Result object with the session details, or null if the session could not be created.</returns>
    public async Task<Session?> AuthenticateWithOAuth2CallbackAsync(string callbackData, CancellationToken cancellationToken = default)
    {
        if (this.sessionManager is not OAuth2SessionManager oAuth2SessionManager)
        {
            throw new OAuth2Exception("Session manager is not an OAuth2 session manager.");
        }

        return await oAuth2SessionManager.CompleteAuthorizationAsync(callbackData, cancellationToken);
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

    private void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                this.sessionManager.Dispose();
            }

            this.disposedValue = true;
        }
    }
}