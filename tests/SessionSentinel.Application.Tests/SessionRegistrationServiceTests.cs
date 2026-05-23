using MediatR;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Services;

namespace SessionSentinel.Application.Tests;

public sealed class SessionRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_forwards_request_to_create_session_command()
    {
        var sender = new FakeSender();
        var service = new SessionRegistrationService(sender);

        await service.RegisterAsync(
            new RegisterSessionRequest(
                "session-1",
                "user-1",
                "127.0.0.1",
                "fingerprint",
                "agent",
                "tr-TR",
                null,
                DateTime.UtcNow));

        Assert.NotNull(sender.Command);
        Assert.Equal("session-1", sender.Command.SessionId);
        Assert.Equal("user-1", sender.Command.UserId);
    }

    private sealed class FakeSender : ISender
    {
        public CreateSessionCommand? Command { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Command = request as CreateSessionCommand;
            return Task.FromResult((TResponse)(object)Unit.Value);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Command = request as CreateSessionCommand;
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
