# MongoDB.AspNetCore.Identity Test Application

This is a test application demonstrating the MongoDB.AspNetCore.Identity library in action.

## Prerequisites

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** - Download and install
2. **MongoDB Server** - Must be running on `localhost:27017`
   - Download from [MongoDB Community Server](https://www.mongodb.com/try/download/community)
   - Or run via Docker: `docker run -d -p 27017:27017 --name mongodb mongo:latest`

## Quick Start

### 1. Install .NET 10 SDK

Download and install from: https://dotnet.microsoft.com/download/dotnet/10.0

### 2. Start MongoDB

Make sure MongoDB is running:

```bash
# Check if MongoDB is running
mongo --eval "db.version()"

# OR if using Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

### 3. Run the Application

From the `TestApplication` directory:

```bash
# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

The application will start and display URLs like:
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

### 4. Open in Browser

Navigate to: **http://localhost:5000** or **https://localhost:5001**

## Features to Test

### Registration
1. Click "Register" in the navigation
2. Fill in the registration form:
   - First Name: John
   - Last Name: Doe
   - Email: john@example.com
   - Password: Test@123
   - Confirm Password: Test@123
3. Click "Register"
4. You should be automatically logged in and redirected to the home page

### Login
1. Click "Login" in the navigation
2. Enter your credentials:
   - Email: john@example.com
   - Password: Test@123
3. Check "Remember me" (optional)
4. Click "Login"

### Secure Page
1. After logging in, click "Secure Page" in the navigation
2. This page requires authentication - you'll see your user information
3. Try accessing it while logged out - you'll be redirected to login

### Logout
1. Click the "Logout" button in the navigation
2. You'll be logged out and redirected to the home page

### Lockout Testing
1. Try logging in with wrong password 5 times
2. The account will be locked out for 5 minutes
3. You'll see the lockout page

## Verify in MongoDB

You can verify the data is stored in MongoDB:

```bash
# Connect to MongoDB
mongosh

# Switch to the database
use IdentityTestDb

# View users collection
db.AspNetUsers.find().pretty()
```

You should see documents like:
```json
{
  "_id": ObjectId("..."),
  "UserName": "john@example.com",
  "NormalizedUserName": "JOHN@EXAMPLE.COM",
  "Email": "john@example.com",
  "NormalizedEmail": "JOHN@EXAMPLE.COM",
  "EmailConfirmed": false,
  "PasswordHash": "...",
  "SecurityStamp": "...",
  "FirstName": "John",
  "LastName": "Doe",
  "TwoFactorEnabled": false,
  "LockoutEnabled": true,
  "AccessFailedCount": 0,
  "Roles": [],
  "Claims": [],
  "Logins": []
}
```

## Configuration

### MongoDB Connection

Edit `appsettings.json` to change MongoDB connection:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "IdentityTestDb"
  }
}
```

### Identity Options

Edit `Program.cs` to change Identity settings:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumericCharacter = false;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // User settings
    options.User.RequireUniqueEmail = true;
})
```

## Troubleshooting

### "Connection refused" error

**Problem:** Can't connect to MongoDB

**Solution:**
1. Make sure MongoDB is running: `mongod --version`
2. Check MongoDB is listening on port 27017
3. Update connection string in `appsettings.json` if using different host/port

### "The type or namespace name 'MongoDB' could not be found"

**Problem:** NuGet packages not restored

**Solution:**
```bash
dotnet restore
dotnet build
```

### Port already in use

**Problem:** Port 5000 or 5001 is already in use

**Solution:** Edit `Properties/launchSettings.json` or use:
```bash
dotnet run --urls "http://localhost:5002"
```

### Can't find the Identity library

**Problem:** Project reference to MongoDB.AspNetCore.Identity not found

**Solution:**
```bash
# From TestApplication.Core directory
cd ..
dotnet build MongoDB.AspNetCore.Identity.csproj
cd TestApplication.Core
dotnet restore
```

## Development

### Hot Reload

The application supports hot reload. Changes to code will automatically reload:

```bash
dotnet watch run
```

### Debug Mode

To run in debug mode:

```bash
dotnet run --configuration Debug
```

## Testing Checklist

- [ ] Register a new user
- [ ] Login with the user
- [ ] Visit secure page (authenticated)
- [ ] Logout
- [ ] Try to visit secure page (should redirect to login)
- [ ] Login with wrong password 5 times (test lockout)
- [ ] Verify user data in MongoDB
- [ ] Register user with same email (should fail)
- [ ] Test password requirements (try weak passwords)

## What This Tests

This application validates:

1. ✅ **User Store Implementation** - All CRUD operations on users
2. ✅ **Password Hashing** - Secure password storage
3. ✅ **Authentication** - Cookie-based authentication
4. ✅ **Authorization** - Protecting pages with `[Authorize]` attribute
5. ✅ **Lockout** - Account lockout after failed attempts
6. ✅ **Async Operations** - All database operations are properly awaited
7. ✅ **MongoDB Integration** - Data persistence in MongoDB
8. ✅ **Custom User Properties** - FirstName and LastName custom fields
9. ✅ **Email Uniqueness** - Duplicate email prevention
10. ✅ **Security Stamps** - Security token generation

## Architecture

```
TestApplication.Core/
├── Controllers/
│   ├── AccountController.cs    # Handles registration, login, logout
│   └── HomeController.cs       # Home and secure pages
├── Models/
│   ├── ApplicationUser.cs      # Custom user model (extends IdentityUser)
│   └── AccountViewModels.cs    # View models for forms
├── Views/
│   ├── Account/                # Login, Register, etc.
│   ├── Home/                   # Home and Secure pages
│   └── Shared/                 # Layout
└── Program.cs                  # App configuration and startup
```

## Next Steps

Once you've tested the application:

1. Review the code in `Program.cs` to see how Identity is configured
2. Check `AccountController.cs` to see how registration and login work
3. Examine `ApplicationUser.cs` to see how to extend the user model
4. Look at MongoDB to understand the document structure
5. Try adding custom claims or roles
6. Implement email confirmation
7. Add two-factor authentication

## Support

For issues with the test application, check:
- MongoDB is running and accessible
- .NET 10 SDK is installed
- All NuGet packages are restored

For issues with the library itself, see the main [README.md](../README.md)

