using Microsoft.EntityFrameworkCore;
using TodoService.Models;

namespace TodoService.Data;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.ToTable("todos"); 
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
        });
    }
}
