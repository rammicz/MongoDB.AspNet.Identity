using Microsoft.AspNetCore.Identity;
using Rammi.MongoDB.AspNetCore.Identity;
using MongoDB.Driver;
using TestApplication.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure MongoDB
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "IdentityTestDb";

Console.WriteLine($"Connecting to MongoDB at: {mongoConnectionString}");
Console.WriteLine($"Database: {mongoDatabaseName}");

var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);

// Register MongoDB database
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Add Identity with MongoDB (without roles for now)
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
})
.AddUserStore<UserStore<ApplicationUser>>()
.AddSignInManager<SignInManager<ApplicationUser>>()
.AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine("Application started successfully!");
Console.WriteLine("Navigate to https://localhost:5001 or http://localhost:5000");

app.Run();

