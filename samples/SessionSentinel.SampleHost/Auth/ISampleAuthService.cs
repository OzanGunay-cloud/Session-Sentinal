namespace SessionSentinel.SampleHost.Auth;

public interface ISampleAuthService
{
    Task<SampleTokenResult?> LoginAsync(
        SampleLoginRequest request,
        SampleLoginContext context,
        CancellationToken cancellationToken = default);
}
