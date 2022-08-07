using Microsoft.EntityFrameworkCore;
using Own.Platform.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(optionsBuilder => optionsBuilder.UseInMemoryDatabase(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddSwagger(typeof(Program), "Identity Api", "Identity Management");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Database.EnsureCreated();
    }
}

app.UseHttpsRedirection();

app.MapPost("/users", async (User user, IdentityDbContext context) =>
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return Results.Created($"/users/{user.UserId}",user);
    })
    .WithName("CreateUser")
    .Produces<User>(StatusCodes.Status201Created);

app.MapGet("/users", async (IdentityDbContext context) =>
    await context.Users.ToListAsync())
    .WithName("GetAllUsers");

app.MapGet("/users/{id}", async (Guid id, IdentityDbContext context) =>
    await context.Users.FindAsync(id)
        is User user
            ? Results.Ok(user)
            : Results.NotFound())
    .WithName("GetUserById")
    .Produces<User>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapPut("/users/{id}", async (Guid id, User inputUser, IdentityDbContext context) =>
    {
        var user = await context.Users.FindAsync(id);

        if (user is null) return Results.NotFound();

        user.Name = inputUser.Name;
        user.Password = inputUser.Password;
        user.Salt = inputUser.Salt;

        await context.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithName("UpdateUser")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);

app.MapDelete("/users/{id}", async (Guid id, IdentityDbContext context) =>
    {
        if (await context.Users.FindAsync(id) is User user)
        {
            context.Users.Remove(user);
            await context.SaveChangesAsync();
            return Results.Ok(user);
        }

        return Results.NotFound();
    })
    .WithName("DeleteUser")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);

app.Run();

public class User
{
    public User()
    {

    }

    public User(string username, string password)
    {

    }

    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public byte[] Salt { get; set; }
}

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
}
