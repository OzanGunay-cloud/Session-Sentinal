using System.Net;
using System.Net.Http;
using System.Text;
using SessionSentinel.Infrastructure.Services;

namespace SessionSentinel.Infrastructure.Tests;

public sealed class IpWhoIsGeoLocationServiceTests
{
    [Fact]
    public async Task ResolveAsync_returns_coordinates_when_provider_payload_is_valid()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "success": true,
              "latitude": 41.0082,
              "longitude": 28.9784
            }
            """);

        var service = new IpWhoIsGeoLocationService(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://ipwho.is/")
        });

        var point = await service.ResolveAsync("8.8.8.8");

        Assert.NotNull(point);
        Assert.Equal(41.0082, point.Value.Latitude);
        Assert.Equal(28.9784, point.Value.Longitude);
    }

    [Fact]
    public async Task ResolveAsync_returns_null_when_provider_denies_lookup()
    {
        var handler = new StubHttpMessageHandler("""{ "success": false }""");
        var service = new IpWhoIsGeoLocationService(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://ipwho.is/")
        });

        var point = await service.ResolveAsync("8.8.8.8");

        Assert.Null(point);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _payload;

        public StubHttpMessageHandler(string payload)
        {
            _payload = payload;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payload, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
