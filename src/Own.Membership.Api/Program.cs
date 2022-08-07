using Microsoft.EntityFrameworkCore;
using Own.Platform.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MembershipDbContext>(optionsBuilder => optionsBuilder.UseInMemoryDatabase(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddSwagger(typeof(Program), "Membership Api", "Membership Management");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();
        db.Database.EnsureCreated();
    }
}

app.UseHttpsRedirection();

app.MapPost("/members", async (Member member, MembershipDbContext context) =>
    {
        context.Members.Add(member);
        await context.SaveChangesAsync();

        return Results.Created($"/members/{member.MemberId}",member);
    })
    .WithName("CreateMember")
    .Produces<Member>(StatusCodes.Status201Created);

app.MapGet("/members", async (MembershipDbContext context) =>
    await context.Members.ToListAsync())
    .WithName("GetAllMembers");

app.MapGet("/members/{id}", async (Guid id, MembershipDbContext context) =>
    await context.Members.FindAsync(id)
        is Member member
            ? Results.Ok(member)
            : Results.NotFound())
    .WithName("GetMemberById")
    .Produces<Member>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapPut("/members/{id}", async (Guid id, Member inputMember, MembershipDbContext context) =>
    {
        var member = await context.Members.FindAsync(id);

        if (member is null) return Results.NotFound();

        member.Name = inputMember.Name;

        await context.SaveChangesAsync();
        return Results.NoContent();
    })
    .WithName("UpdateMember")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);

app.MapDelete("/members/{id}", async (Guid id, MembershipDbContext context) =>
    {
        if (await context.Members.FindAsync(id) is Member member)
        {
            context.Members.Remove(member);
            await context.SaveChangesAsync();
            return Results.Ok(member);
        }

        return Results.NotFound();
    })
    .WithName("DeleteMember")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);

app.Run();

public class Member
{
    public Guid MemberId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MembershipDbContext : DbContext
{
    public MembershipDbContext(DbContextOptions<MembershipDbContext> options)
        : base(options) { }

    public DbSet<Member> Members => Set<Member>();
}
