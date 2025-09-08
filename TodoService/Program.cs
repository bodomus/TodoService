using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using TodoService.Data;
using TodoService.Models;

var builder = WebApplication.CreateBuilder(args);

var connStr =
    Environment.GetEnvironmentVariable("POSTGRES_CONN") ??
    builder.Configuration.GetConnectionString("Default") ??
    "Host=postgres;Port=5432;Database=todos;Username=postgres;Password=postgres";

builder.Services.AddDbContext<TodoDbContext>(opt => opt.UseNpgsql(connStr));

// API versioning
// Версионирование
builder.Services
    .AddApiVersioning(o =>
    {
        o.DefaultApiVersion = new ApiVersion(1, 0);
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ReportApiVersions = true;
    })
    // 👇 AddApiExplorer вызываем ИМЕННО на результате AddApiVersioning (IApiVersioningBuilder)
    .AddApiExplorer(o =>
    {
        o.GroupNameFormat = "'v'VVV";         // v1, v2
        o.SubstituteApiVersionInUrl = true;   // /v{version}/...
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();


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

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
#region todos

// app.MapGet("/todos", async (TodoDbContext db) =>
//     Results.Ok(await db.Todos.AsNoTracking().OrderBy(t => t.Id).ToListAsync()));


var v1Set = app.NewApiVersionSet("Todos v1")
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var v2Set = app.NewApiVersionSet("Todos v2")
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

app.MapGet("/v{version:apiVersion}/todos", 
        async (TodoDbContext db) => Results.Ok(await db.Todos.AsNoTracking().OrderBy(t => t.Id).ToListAsync()))
    .WithApiVersionSet(v1Set).MapToApiVersion(new ApiVersion(1, 0));

app.MapGet("/v{version:apiVersion}/todos",
        async (TodoDbContext db) => Results.Ok(await db.Todos.AsNoTracking().OrderBy(t => t.Id).ToListAsync()))
    .WithApiVersionSet(v2Set).MapToApiVersion(new ApiVersion(2, 0));

#endregion

#region todos by id

app.MapGet("/v{version:apiVersion}/todos/{id:int}", 
        async (int id, TodoDbContext db) => await db.Todos.FindAsync(id) is { } todo ? Results.Ok(todo) : Results.NotFound())
    .WithApiVersionSet(v1Set).MapToApiVersion(new ApiVersion(1, 0));

app.MapGet("/v{version:apiVersion}/todos/{id:int}",
        async (int id, TodoDbContext db) => await db.Todos.FindAsync(id) is { } todo ? Results.Ok(todo) : Results.NotFound())
    .WithApiVersionSet(v2Set).MapToApiVersion(new ApiVersion(2, 0));



#endregion

#region Post todo


app.MapPost("/v{version:apiVersion}/todos", async (CreateTodoDto dto, TodoDbContext db) =>
{
    var todo = new Todo { Title = dto.Title, IsCompleted = dto.IsCompleted };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created("/v{version:apiVersion}" + $"/todos/{todo.Id}", todo);
}).WithApiVersionSet(v1Set).MapToApiVersion(new ApiVersion(1, 0))
.Produces<Todo>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);


app.MapPost("/v{version:apiVersion}/todos", async (CreateTodoDto dto, TodoDbContext db) =>
    {
        var todo = new Todo { Title = dto.Title, IsCompleted = dto.IsCompleted };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return Results.Created("/v{version:apiVersion}" + $"/todos/{todo.Id}", todo);
    }).WithApiVersionSet(v2Set).MapToApiVersion(new ApiVersion(2, 0))
    .Produces<Todo>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest);

#endregion

#region Put todo


app.MapPut("/v{version:apiVersion}/todos/{id:int}", async (int id, Todo updated, TodoDbContext db) =>
{
    if (id != updated.Id) return Results.BadRequest("Id mismatch");
    var exists = await db.Todos.AnyAsync(t => t.Id == id);
    if (!exists) return Results.NotFound();
    db.Todos.Update(updated);
    await db.SaveChangesAsync();
    return Results.Ok(updated);
}).WithApiVersionSet(v1Set).MapToApiVersion(new ApiVersion(1, 0));

app.MapPut("/v{version:apiVersion}/todos/{id:int}", async (int id, Todo updated, TodoDbContext db) =>
{
    if (id != updated.Id) return Results.BadRequest("Id mismatch");
    var exists = await db.Todos.AnyAsync(t => t.Id == id);
    if (!exists) return Results.NotFound();
    db.Todos.Update(updated);
    await db.SaveChangesAsync();
    return Results.Ok(updated);
}).WithApiVersionSet(v2Set).MapToApiVersion(new ApiVersion(2, 0));

#endregion

#region delete todo


app.MapDelete("/v{version:apiVersion}/todos/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithApiVersionSet(v1Set).MapToApiVersion(new ApiVersion(1, 0));

app.MapDelete("/v{version:apiVersion}/todos/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithApiVersionSet(v2Set).MapToApiVersion(new ApiVersion(2, 0));
#endregion

var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger"; // страница UI по /swagger
        // foreach (var description in provider.ApiVersionDescriptions)
        // {
        //     options.SwaggerEndpoint(
        //         $"/swagger/{description.GroupName}/swagger.json",
        //         $"TodoService {description.GroupName.ToUpperInvariant()}");
        // }
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoService V1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "TodoService V2");
    });
}

app.Run();
public partial class Program { }
public record CreateTodoDto(string Title, bool IsCompleted);
