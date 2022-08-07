using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(optionsBuilder => optionsBuilder.UseInMemoryDatabase(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "",
        Description = "",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "",
            Email = ""
        },
        License = new OpenApiLicense
        {
            Name = "Use under MIT",
            Url = new Uri("https://opensource.org/licenses/MIT"),
        }
    });

    options.EnableAnnotations();

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

}).AddSwaggerGenNewtonsoftSupport();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.SerializeAsV2 = true);
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "");
        options.RoutePrefix = string.Empty;
        options.DisplayOperationId();
    });

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
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Password { get; set; }
    public byte[] Salt { get; set; }
}

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
}
