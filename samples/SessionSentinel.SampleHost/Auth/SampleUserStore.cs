namespace SessionSentinel.SampleHost.Auth;

public sealed class SampleUserStore
{
    private static readonly IReadOnlyList<SampleUser> Users =
    [
        new("demo-user", "demo", "demo123"),
        new("admin-user", "admin", "admin123")
    ];

    // The sample keeps credentials in memory to stay dependency-free.
    public SampleUser? Validate(string userName, string password) =>
        Users.FirstOrDefault(user =>
            string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(user.Password, password, StringComparison.Ordinal));
}
