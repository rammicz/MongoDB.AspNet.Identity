# Migration Guide: MongoDB.AspNet.Identity to MongoDB.AspNetCore.Identity

This guide helps you migrate from the old `MongoDB.AspNet.Identity` (v1.x for .NET Framework) to the new `MongoDB.AspNetCore.Identity` (v2.x for .NET 10.0).

## Overview of Changes

| Aspect | Old (v1.x) | New (v2.x) |
|--------|-----------|-----------|
| Target Framework | .NET Framework 4.5.2 | .NET 10 |
| Package Name | `MongoDB.AspNet.Identity` | `MongoDB.AspNetCore.Identity` |
| Namespace | `MongoDB.AspNet.Identity` | `MongoDB.AspNetCore.Identity` |
| Identity Framework | `Microsoft.AspNet.Identity.Core` 2.2.1 | `Microsoft.AspNetCore.Identity` 2.2.0 |
| MongoDB Driver | 2.10.1 | 3.1.0 |
| Async/Await | Incorrectly implemented | Properly implemented |

## Breaking Changes

### 1. Namespace Change

**Old:**
```csharp
using MongoDB.AspNet.Identity;
```

**New:**
```csharp
using MongoDB.AspNetCore.Identity;
```

### 2. Identity User Base Class

The `IdentityUser` class now inherits from `Microsoft.AspNetCore.Identity.IdentityUser<string>` instead of implementing `IUser<string>`.

**Old structure:**
```csharp
public class IdentityUser : IUser<string>
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string PasswordHash { get; set; }
    public string SecurityStamp { get; set; }
    public List<string> Roles { get; private set; }
    public List<IdentityUserClaim> Claims { get; private set; }
    public List<UserLoginInfo> Logins { get; private set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? LockoutEndDateUtc { get; set; }
    public int AccessFailedCount { get; set; }
    public bool LockoutEnabled { get; set; }
    public bool TwoFactorEnabled { get; set; }
}
```

**New structure:**
```csharp
public class IdentityUser : Microsoft.AspNetCore.Identity.IdentityUser<string>
{
    // Inherits all standard Identity properties
    // Id is decorated with [BsonId] and [BsonRepresentation(BsonType.ObjectId)]
}
```

All the standard Identity properties are now inherited from the base class.

### 3. UserStore Constructor Changes

**Old:**
```csharp
// Constructor with connection string name from web.config
var userStore = new UserStore<IdentityUser>("DefaultConnection");

// Constructor with MongoDB URL
var userStore = new UserStore<IdentityUser>("mongodb://localhost/mydb");

// Constructor with connection string name and database name
var userStore = new UserStore<IdentityUser>("ConnectionName", "DatabaseName");

// Constructor with IMongoDatabase
var userStore = new UserStore<IdentityUser>(mongoDatabase);
```

**New:**
```csharp
// Constructor with IMongoDatabase (recommended)
var userStore = new UserStore<IdentityUser>(mongoDatabase);

// Constructor with connection string and database name
var userStore = new UserStore<IdentityUser>("mongodb://localhost:27017", "mydb");
```

**Note:** The new version no longer reads from `web.config` or `appsettings.json` directly. You need to configure the MongoDB connection yourself.

### 4. Service Registration

**Old (ASP.NET MVC 5):**
```csharp
// In IdentityConfig.cs or Startup.Auth.cs
public static ApplicationUserManager Create(
    IdentityFactoryOptions<ApplicationUserManager> options,
    IOwinContext context)
{
    var manager = new ApplicationUserManager(
        new UserStore<ApplicationUser>("DefaultConnection"));
    
    // Configure manager...
    return manager;
}
```

**New (ASP.NET Core):**
```csharp
// In Program.cs
using MongoDB.AspNetCore.Identity;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]!;
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"]!;
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);

// Register MongoDB database as singleton
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// Register Identity with MongoDB UserStore
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddUserStore<UserStore<IdentityUser>>()
    .AddDefaultTokenProviders();

// Configure Identity options (optional)
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.User.RequireUniqueEmail = true;
});
```

### 5. Application Configuration

**Old (web.config):**
```xml
<connectionStrings>
  <add name="DefaultConnection" 
       connectionString="mongodb://localhost/mydb" />
</connectionStrings>
```

**New (appsettings.json):**
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "mydb"
  }
}
```

## Step-by-Step Migration

### Step 1: Upgrade to .NET 10

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Create a new ASP.NET Core 10 project or migrate your existing project
3. Update your `.csproj` file to target `net10.0`

### Step 2: Update Package References

**Remove old packages:**
```bash
dotnet remove package MongoDB.AspNet.Identity
dotnet remove package Microsoft.AspNet.Identity.Core
dotnet remove package Microsoft.AspNet.Identity.Owin
```

**Add new packages:**
```bash
dotnet add package MongoDB.AspNetCore.Identity
```

### Step 3: Update Namespaces

Find and replace throughout your project:
- `using MongoDB.AspNet.Identity;` → `using MongoDB.AspNetCore.Identity;`
- `using Microsoft.AspNet.Identity;` → `using Microsoft.AspNetCore.Identity;`

### Step 4: Update Identity User Model

**Old:**
```csharp
using MongoDB.AspNet.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

**New:**
```csharp
using MongoDB.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
```

The base class changed, but your custom properties remain the same.

### Step 5: Update MongoDB Connection Setup

**Old (Global.asax.cs or Startup.cs):**
```csharp
// Connection configured via web.config, instantiated per request
```

**New (Program.cs):**
```csharp
using MongoDB.Driver;

var mongoClient = new MongoClient("mongodb://localhost:27017");
var mongoDatabase = mongoClient.GetDatabase("mydb");
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
```

### Step 6: Update Identity Configuration

**Old (IdentityConfig.cs):**
```csharp
public class ApplicationUserManager : UserManager<ApplicationUser>
{
    public ApplicationUserManager(IUserStore<ApplicationUser> store)
        : base(store)
    {
    }

    public static ApplicationUserManager Create(
        IdentityFactoryOptions<ApplicationUserManager> options,
        IOwinContext context)
    {
        var manager = new ApplicationUserManager(
            new UserStore<ApplicationUser>("DefaultConnection"));
        
        // Configure validation logic
        manager.UserValidator = new UserValidator<ApplicationUser>(manager)
        {
            AllowOnlyAlphanumericUserNames = false,
            RequireUniqueEmail = true
        };

        manager.PasswordValidator = new PasswordValidator
        {
            RequiredLength = 6,
            RequireNonLetterOrDigit = true,
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
        };

        return manager;
    }
}
```

**New (Program.cs):**
```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumericCharacter = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddUserStore<UserStore<ApplicationUser>>()
.AddDefaultTokenProviders();
```

### Step 7: Update Controllers

**Old (AccountController.cs):**
```csharp
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

public class AccountController : Controller
{
    private ApplicationUserManager _userManager;

    public ApplicationUserManager UserManager
    {
        get
        {
            return _userManager ?? HttpContext.GetOwinContext()
                .GetUserManager<ApplicationUserManager>();
        }
        private set { _userManager = value; }
    }

    [HttpPost]
    public async Task<ActionResult> Register(RegisterViewModel model)
    {
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await UserManager.CreateAsync(user, model.Password);
        // ...
    }
}
```

**New (AccountController.cs):**
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);
        // ...
    }
}
```

## Database Compatibility

**Good news!** Your existing MongoDB data is **compatible** with the new version. The collection structure remains the same:

- Collection name: `AspNetUsers`
- Document structure is compatible
- No data migration needed

However, you may want to verify that:
1. `NormalizedUserName` field exists and is populated
2. `NormalizedEmail` field exists and is populated

If these fields are missing, the new version will populate them automatically when users are updated.

## Testing Your Migration

### 1. Create a test environment

```bash
# Clone your production database for testing
mongodump --uri="mongodb://production-server/mydb" --out=./backup
mongorestore --uri="mongodb://test-server/mydb-test" ./backup/mydb
```

### 2. Test user operations

```csharp
// Test user creation
var user = new ApplicationUser { UserName = "test@example.com", Email = "test@example.com" };
var result = await _userManager.CreateAsync(user, "Test@123");
Assert.True(result.Succeeded);

// Test user login
var signInResult = await _signInManager.PasswordSignInAsync(
    "test@example.com", 
    "Test@123", 
    false, 
    false);
Assert.True(signInResult.Succeeded);

// Test user update
user.FirstName = "Test";
var updateResult = await _userManager.UpdateAsync(user);
Assert.True(updateResult.Succeeded);

// Test roles
await _userManager.AddToRoleAsync(user, "Admin");
var isInRole = await _userManager.IsInRoleAsync(user, "Admin");
Assert.True(isInRole);
```

## Common Issues and Solutions

### Issue 1: Users can't log in after migration

**Cause:** Username normalization mismatch

**Solution:**
```csharp
// Add this code to populate normalized fields for existing users
public async Task MigrateExistingUsers()
{
    var collection = _database.GetCollection<ApplicationUser>("AspNetUsers");
    var users = await collection.Find(_ => true).ToListAsync();
    
    foreach (var user in users)
    {
        user.NormalizedUserName = user.UserName?.ToUpperInvariant();
        user.NormalizedEmail = user.Email?.ToUpperInvariant();
        
        await collection.ReplaceOneAsync(
            u => u.Id == user.Id,
            user);
    }
}
```

### Issue 2: Duplicate key errors

**Cause:** The new version creates unique indexes on `NormalizedUserName`

**Solution:** Ensure all usernames are unique before migration

### Issue 3: LockoutEndDateUtc errors

**Cause:** Field name changed from `LockoutEndDateUtc` to `LockoutEnd`

**Solution:** The new `IdentityUser` base class uses `LockoutEnd` which is a `DateTimeOffset?`. The old data will be automatically mapped.

## Rollback Plan

If you need to rollback:

1. Restore your MongoDB backup
2. Restore your old application code
3. Redeploy

Your database structure hasn't changed, so rollback is safe.

## Support and Help

- Open an issue on [GitHub](https://github.com/InspectorIT/MongoDB.AspNet.Identity/issues)
- Check the [README](README.md) for updated usage examples
- Review [ASP.NET Core Identity documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)

## Summary Checklist

- [ ] Upgrade to .NET 10
- [ ] Update package references
- [ ] Update all `using` statements
- [ ] Update service registration in Program.cs
- [ ] Update MongoDB connection configuration
- [ ] Update Identity configuration
- [ ] Update controllers to use dependency injection
- [ ] Test user creation, login, and updates
- [ ] Test role and claim operations
- [ ] Test lockout functionality
- [ ] Deploy to staging environment
- [ ] Run integration tests
- [ ] Backup production database
- [ ] Deploy to production
- [ ] Monitor for issues

Good luck with your migration! 🚀

