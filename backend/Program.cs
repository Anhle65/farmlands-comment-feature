using CommentApi.Data;
using CommentApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCors = "AllowFrontend";

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<CommentStore>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy =>
        policy.WithOrigins("http://localhost:5173") // Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod());
});
builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseSqlite("Data Source=Comments.db"));
var app = builder.Build();

// Apply pending migrations (creates Comments.db on first run) and seed demo data
// if the table is empty. Both are idempotent on subsequent startups.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommentDbContext>();
    db.Database.Migrate();
    SeedIfEmpty(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCors);

app.UseAuthorization();

app.MapControllers();

app.Run();

// Seed mock comments on first run only. Timestamps are relative to UtcNow so the
// 5-minute edit window stays demoable on whichever day the app is launched.
static void SeedIfEmpty(CommentDbContext db)
{
    if (db.Comments.Any()) return;

    const string AuthorBob   = "11111111-1111-1111-1111-111111111111";
    const string AuthorEve   = "22222222-2222-2222-2222-222222222222";
    const string AuthorSam   = "33333333-3333-3333-3333-333333333333";
    const string AuthorAlice = "44444444-4444-4444-4444-444444444444";

    var now = DateTimeOffset.UtcNow;

    // Insert Bob first so we can reference his database-assigned Id when Eve replies.
    var bob = new Comment
    {
        AuthorId = AuthorBob, AuthorName = "Bob",
        Content = "What a nice weather for our community picnic!",
        CreatedAt = now.AddDays(-3),
    };
    db.Comments.Add(bob);
    db.SaveChanges();

    db.Comments.AddRange(
        new Comment
        {
            AuthorId = AuthorEve, AuthorName = "Eve",
            Content = "Agreed, it's a beautiful day for a walk in the park.",
            CreatedAt = now.AddDays(-2), ParentId = bob.Id,
        },
        new Comment
        {
            AuthorId = AuthorAlice, AuthorName = "Alice",
            Content = "Any other activities planned for the weekend?",
            CreatedAt = now.AddDays(-1), UpdatedAt = now.AddHours(-23),
        },
        new Comment
        {
            AuthorId = AuthorSam, AuthorName = "Sam",
            Content = "Weather forecast says it might rain on the weekend.",
            CreatedAt = now.AddHours(-5), IsDeleted = true,
        }
    );
    db.SaveChanges();
}
