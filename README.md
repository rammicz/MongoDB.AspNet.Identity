# MongoDB.AspNetCore.Identity

ASP.NET Core Identity provider that uses MongoDB for storage

## Overview

This library provides a MongoDB-based implementation of ASP.NET Core Identity, offering a modern alternative to Entity Framework-based storage. Built for **.NET 10** with the latest MongoDB driver (3.1.0), it provides full async/await support and proper error handling.

## Features

- ✅ **Modern .NET 10** support
- ✅ **Latest MongoDB Driver 3.1.0** with full async/await
- ✅ **Single MongoDB collection** (AspNetUsers) - simpler than EF's multi-table approach
- ✅ **Full ASP.NET Core Identity integration**
- ✅ **Nullable reference types** support
- ✅ Implements all standard Identity interfaces:
  - `IUserStore<TUser>`
  - `IUserLoginStore<TUser>`
  - `IUserRoleStore<TUser>`
  - `IUserClaimStore<TUser>`
  - `IUserPasswordStore<TUser>`
  - `IUserSecurityStampStore<TUser>`
  - `IUserEmailStore<TUser>`
  - `IUserPhoneNumberStore<TUser>`
  - `IUserLockoutStore<TUser>`
  - `IUserTwoFactorStore<TUser>`

## Installation

```bash
dotnet add package MongoDB.AspNetCore.Identity
```

Or via Package Manager Console:

```powershell
Install-Package MongoDB.AspNetCore.Identity
```

## Quick Start

### 1. Configure Services

In your `Program.cs`:

```csharp
using MongoDB.AspNetCore.Identity;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB
var mongoClient = new MongoClient("mongodb://localhost:27017");
var mongoDatabase = mongoClient.GetDatabase("YourDatabaseName");

// Add Identity with MongoDB
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddScoped<UserStore<IdentityUser>>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddUserStore<UserStore<IdentityUser>>()
    .AddDefaultTokenProviders();

var app = builder.Build();
```

### 2. Using Connection String from Configuration

In `appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "YourDatabaseName"
  }
}
```

In `Program.cs`:

```csharp
using MongoDB.AspNetCore.Identity;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]!;
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"]!;

var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);

builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddUserStore<UserStore<IdentityUser>>()
    .AddDefaultTokenProviders();
```

### 3. Custom User Model

Create your own user class:

```csharp
using MongoDB.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
}
```

Then use it in your configuration:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddUserStore<UserStore<ApplicationUser>>()
    .AddDefaultTokenProviders();
```

## Usage Examples

### Register a User

```csharp
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }
}
```

### Login

```csharp
[HttpPost]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (ModelState.IsValid)
    {
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: false);
            
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }
        
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
    }
    return View(model);
}
```

## Migration from Old Version

If you're migrating from the old `MongoDB.AspNet.Identity` package (for .NET Framework):

### Key Changes:

1. **Package name changed**: `MongoDB.AspNet.Identity` → `MongoDB.AspNetCore.Identity`
2. **Namespace changed**: `MongoDB.AspNet.Identity` → `MongoDB.AspNetCore.Identity`
3. **Target framework**: .NET Framework 4.5.2 → .NET 10
4. **Identity system**: `Microsoft.AspNet.Identity` → `Microsoft.AspNetCore.Identity`
5. **Proper async/await**: All database operations now properly await MongoDB operations

### Migration Steps:

1. Update to .NET 10
2. Replace package references
3. Update namespace imports
4. Update service registration (see Quick Start above)
5. The MongoDB collection structure remains compatible

## Connection String Formats

### Standard MongoDB Connection String

```
mongodb://localhost:27017
```

### With Authentication

```
mongodb://username:password@localhost:27017
```

### MongoDB Atlas

```
mongodb+srv://username:password@cluster.mongodb.net/
```

## Requirements

- .NET 10 or later
- MongoDB Server 4.0 or later (5.0+ recommended)
- MongoDB.Driver 3.1.0 or later

## Performance Tips

1. **Indexes**: The library automatically creates indexes on `NormalizedUserName` (unique) and `NormalizedEmail`
2. **Connection pooling**: Reuse `MongoClient` instances (registered as Singleton)
3. **Async all the way**: All operations are properly async - don't use `.Result` or `.Wait()`

## Troubleshooting

### Duplicate Key Error on User Creation

This means the username already exists. The library catches this and returns a proper `IdentityResult.Failed` with a "DuplicateUserName" error code.

### User Not Found

Make sure your MongoDB connection string and database name are correct. Check that the `AspNetUsers` collection exists in your database.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License - See LICENSE file for details

## Credits

- Original project by [jsheely](https://github.com/jsheely) and sitebro
- Updated to .NET 10 and modern standards by the community
- Special thanks to [David Boike](https://github.com/DavidBoike) whose [RavenDB AspNet Identity](https://github.com/ILMServices/RavenDB.AspNet.Identity) project provided the initial foundation

## Support

For issues and questions:
- Open an issue on [GitHub](https://github.com/rammicz/MongoDB.AspNet.Identity/issues)
- Check existing issues for solutions

## Version History

### 2.0.0 (2025)
- Migrated to .NET 10
- Updated to MongoDB.Driver 3.1.0
- Migrated from Microsoft.AspNet.Identity to Microsoft.AspNetCore.Identity
- Fixed all async/await issues for proper asynchronous operations
- Modern SDK-style project format
- Full nullable reference types support
- Proper error handling and validation
- Breaking changes: See Migration section

### 1.0.7 (2018)
- Last version for .NET Framework
- Added email, lockout, and two-factor authentication support
