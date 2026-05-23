using MediatR;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Services;

namespace SessionSentinel.Application.Tests;

public sealed class SessionRevocationServiceTests
{
    [Fact]
    public async Task RevokeAsync_forwards_request_to_revoke_session_command()
    {
        var sender = new FakeSender();
        var service = new SessionRevocationService(sender);

        await service.RevokeAsync(
            new RevokeSessionRequest(
                "session-1",
                "user-1",
                "token-hash",
                DateTimeOffset.UtcNow.AddMinutes(30),
                "User logout",
                DateTime.UtcNow));

        Assert.NotNull(sender.Command);
        Assert.Equal("session-1", sender.Command.SessionId);
        Assert.Equal("user-1", sender.Command.UserId);
        Assert.Equal("User logout", sender.Command.Reason);
    }

    private sealed class FakeSender : ISender
    {
        public RevokeSessionCommand? Command { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Command = request as RevokeSessionCommand;
            return Task.FromResult((TResponse)(object)Unit.Value);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Command = request as RevokeSessionCommand;
            return Task.FromResult<object?>(Unit.Value);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
