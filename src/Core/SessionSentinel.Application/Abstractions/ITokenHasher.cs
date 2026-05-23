namespace SessionSentinel.Application.Abstractions;

public interface ITokenHasher
{
    string HashToken(string token);
}
