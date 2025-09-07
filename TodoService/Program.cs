// ReSharper disable RedundantAnonymousTypePropertyName
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TodoService.Data;
using TodoService.Models;

var builder = WebApplication.CreateBuilder(args);

var connStr =
    Environment.GetEnvironmentVariable("POSTGRES_CONN") ??
    builder.Configuration.GetConnectionString("Default") ??
    "Host=postgres;Port=5432;Database=todos;Username=postgres;Password=postgres";

builder.Services.AddDbContext<TodoDbContext>(opt => opt.UseNpgsql(connStr));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    var c = db.Todos;
    db.Database.Migrate();
    // --- SEED ON STARTUP (dev only) ---
    var seed = Environment.GetEnvironmentVariable("SEED") ?? "true"; // можно выключить через SEED=false
    if (bool.TryParse(seed, out var doSeed) && doSeed)
    {
        if (!db.Todos.Any())
        {
            db.Todos.AddRange(
                new Todo { Title = "Купить кофе",    IsCompleted = false },
                new Todo { Title = "Проверить Swagger", IsCompleted = false },
                new Todo { Title = "Написать юнит-тест", IsCompleted = true }
            );
            await db.SaveChangesAsync();
        }
    }

}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/todos", async (TodoDbContext db) =>
    Results.Ok(await db.Todos.AsNoTracking().OrderBy(t => t.Id).ToListAsync()));

app.MapGet("/todos/{id:int}", async (int id, TodoDbContext db) =>
    await db.Todos.FindAsync(id) is { } todo ? Results.Ok(todo) : Results.NotFound());

app.MapPost("/todos", async (CreateTodoDto dto, TodoDbContext db) =>
{
    var todo = new Todo { Title = dto.Title, IsCompleted = dto.IsCompleted };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", todo);
})
.Produces<Todo>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

app.MapPut("/todos/{id:int}", async (int id, Todo updated, TodoDbContext db) =>
{
    if (id != updated.Id) return Results.BadRequest("Id mismatch");
    var exists = await db.Todos.AnyAsync(t => t.Id == id);
    if (!exists) return Results.NotFound();
    db.Todos.Update(updated);
    await db.SaveChangesAsync();
    return Results.Ok(updated);
});

app.MapDelete("/todos/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
public partial class Program { }
public record CreateTodoDto(string Title, bool IsCompleted);
