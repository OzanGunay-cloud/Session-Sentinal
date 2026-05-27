using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;

namespace SessionSentinel.SampleHost.Auth;

public sealed class SampleAuthService : ISampleAuthService
{
    private readonly SampleUserStore _userStore;
    private readonly SampleJwtTokenService _tokenService;
    private readonly SampleIssuedSessionStore _issuedSessionStore;
    private readonly ISessionRegistrationService _sessionRegistrationService;

    public SampleAuthService(
        SampleUserStore userStore,
        SampleJwtTokenService tokenService,
        SampleIssuedSessionStore issuedSessionStore,
        ISessionRegistrationService sessionRegistrationService)
    {
        _userStore = userStore;
        _tokenService = tokenService;
        _issuedSessionStore = issuedSessionStore;
        _sessionRegistrationService = sessionRegistrationService;
    }

    public async Task<SampleTokenResult?> LoginAsync(
        SampleLoginRequest request,
        SampleLoginContext context,
        CancellationToken cancellationToken = default)
    {
        var user = _userStore.Validate(request.UserName, request.Password);
        if (user is null)
        {
            return null;
        }

        var token = _tokenService.CreateToken(user);

        // Register the session at login so later requests already have a baseline snapshot.
        await _sessionRegistrationService.RegisterAsync(
            new RegisterSessionRequest(
                token.SessionId,
                user.UserId,
                context.IpAddress,
                request.FingerprintHash,
                context.UserAgent,
                context.Language,
                null,
                context.RequestedAtUtc),
            cancellationToken);

        _issuedSessionStore.Store(
            new IssuedSessionRecord(
                token.SessionId,
                user.UserId,
                user.UserName,
                token.AccessToken,
                token.ExpiresAtUtc));

        return token;
    }
}
