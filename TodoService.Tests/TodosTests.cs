using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using TodoService.Models;
using Xunit;

namespace TodoService.Tests;
public class TodosTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;
    public TodosTests(AppFactory factory) { _factory = factory; }
    [Fact]
    public async Task Can_create_and_get_todo()
    {
        var client = _factory.CreateClient();
        var todo = new Todo { Title = "Test", IsCompleted = false };
        var post = await client.PostAsJsonAsync("v1/todos", todo);
        var id = post.Headers.Location.OriginalString.Split("/");
        post.StatusCode.Should().Be(HttpStatusCode.Created);
        var get = await client.GetAsync($"v1/todos/{int.Parse(id.Last())}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var obj = await get.Content.ReadFromJsonAsync<Todo>();
        obj!.Title.Should().Be("Test");
    }
}
