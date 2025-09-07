using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace TodoService.Tests;
public class HealthTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;
    public HealthTests(AppFactory factory) { _factory = factory; }
    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("ok");
    }
}
