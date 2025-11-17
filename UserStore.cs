using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Rammi.MongoDB.AspNetCore.Identity;

/// <summary>
/// MongoDB implementation of ASP.NET Core Identity UserStore
/// </summary>
/// <typeparam name="TUser">The type of user</typeparam>
public class UserStore<TUser> :
    IUserStore<TUser>,
    IUserLoginStore<TUser>,
    IUserClaimStore<TUser>,
    IUserRoleStore<TUser>,
    IUserPasswordStore<TUser>,
    IUserSecurityStampStore<TUser>,
    IUserEmailStore<TUser>,
    IUserLockoutStore<TUser>,
    IUserTwoFactorStore<TUser>,
    IUserPhoneNumberStore<TUser>
    where TUser : IdentityUser
{
    private readonly IMongoCollection<TUser> _usersCollection;
    private bool _disposed;
    private const string CollectionName = "AspNetUsers";

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UserStore{TUser}"/> class using a MongoDB database instance.
    /// </summary>
    /// <param name="database">The MongoDB database instance.</param>
    public UserStore(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _usersCollection = database.GetCollection<TUser>(CollectionName);
        EnsureIndexes();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserStore{TUser}"/> class using a connection string.
    /// </summary>
    /// <param name="connectionString">MongoDB connection string.</param>
    /// <param name="databaseName">Database name.</param>
    public UserStore(string connectionString, string databaseName)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentException.ThrowIfNullOrEmpty(databaseName);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _usersCollection = database.GetCollection<TUser>(CollectionName);
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        // Create indexes for common queries (fire-and-forget with error handling)
        var indexKeysDefinition = Builders<TUser>.IndexKeys.Ascending(u => u.NormalizedUserName);
        var indexModel = new CreateIndexModel<TUser>(indexKeysDefinition, new CreateIndexOptions { Unique = true });
        _ = _usersCollection.Indexes.CreateOneAsync(indexModel).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Log error if needed, but don't throw - index creation failures are non-critical
                // Indexes will be created on next access or can be created manually
            }
        }, TaskContinuationOptions.OnlyOnFaulted);

        var emailIndexKeys = Builders<TUser>.IndexKeys.Ascending(u => u.NormalizedEmail);
        var emailIndexModel = new CreateIndexModel<TUser>(emailIndexKeys);
        _ = _usersCollection.Indexes.CreateOneAsync(emailIndexModel).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Log error if needed, but don't throw - index creation failures are non-critical
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    #endregion

    #region IUserStore

    public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _usersCollection.InsertOneAsync(user, cancellationToken: cancellationToken);
            return IdentityResult.Success;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateUserName",
                Description = "Username is already taken."
            });
        }
    }

    public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _usersCollection.DeleteOneAsync(
            Builders<TUser>.Filter.Eq(u => u.Id, user.Id),
            cancellationToken);

        return result.DeletedCount > 0
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Description = "User not found." });
    }

    public async Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!ObjectId.TryParse(userId, out _))
        {
            return null;
        }

        return await _usersCollection
            .Find(Builders<TUser>.Filter.Eq(u => u.Id, userId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        return await _usersCollection
            .Find(Builders<TUser>.Filter.Eq(u => u.NormalizedUserName, normalizedUserName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.Id ?? string.Empty);
    }

    public Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.UserName);
    }

    public Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _usersCollection.ReplaceOneAsync(
            Builders<TUser>.Filter.Eq(u => u.Id, user.Id),
            user,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);

        return result.ModifiedCount > 0 || result.MatchedCount > 0
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Description = "User not found." });
    }

    #endregion

    #region IUserPasswordStore

    public Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.PasswordHash != null);
    }

    public Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserSecurityStampStore

    public Task<string?> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.SecurityStamp);
    }

    public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserEmailStore

    public Task<string?> GetEmailAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task<string?> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetEmailAsync(TUser user, string? email, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(TUser user, string? normalizedEmail, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public async Task<TUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        return await _usersCollection
            .Find(Builders<TUser>.Filter.Eq(u => u.NormalizedEmail, normalizedEmail))
            .FirstOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region IUserPhoneNumberStore

    public Task<string?> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task SetPhoneNumberAsync(TUser user, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserTwoFactorStore

    public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.TwoFactorEnabled);
    }

    public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserLockoutStore

    public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.LockoutEnd);
    }

    public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserClaimStore

    public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        IList<Claim> claims = user.Claims.Select(c => new Claim(c.ClaimType!, c.ClaimValue!)).ToList();
        return Task.FromResult(claims);
    }

    public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(claims);

        foreach (var claim in claims)
        {
            if (!user.Claims.Any(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value))
            {
                user.Claims.Add(new IdentityUserClaim<string>
                {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }
        }
        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(newClaim);

        var existingClaim = user.Claims.FirstOrDefault(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
        if (existingClaim != null)
        {
            existingClaim.ClaimType = newClaim.Type;
            existingClaim.ClaimValue = newClaim.Value;
        }
        return Task.CompletedTask;
    }

    public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(claims);

        foreach (var claim in claims)
        {
            user.Claims.RemoveAll(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
        }
        return Task.CompletedTask;
    }

    public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(claim);
        cancellationToken.ThrowIfCancellationRequested();

        var filter = Builders<TUser>.Filter.And(
            Builders<TUser>.Filter.ElemMatch(u => u.Claims, c => c.ClaimType == claim.Type),
            Builders<TUser>.Filter.ElemMatch(u => u.Claims, c => c.ClaimValue == claim.Value)
        );

        return await _usersCollection.Find(filter).ToListAsync(cancellationToken);
    }

    #endregion

    #region IUserRoleStore

    public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(roleName);

        var normalizedRole = roleName.ToUpperInvariant();
        if (!user.Roles.Contains(normalizedRole, StringComparer.OrdinalIgnoreCase))
        {
            user.Roles.Add(normalizedRole);
        }
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(roleName);

        user.Roles.RemoveAll(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult<IList<string>>(user.Roles);
    }

    public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(roleName);

        return Task.FromResult(user.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(roleName);
        cancellationToken.ThrowIfCancellationRequested();

        var filter = Builders<TUser>.Filter.AnyEq(u => u.Roles, roleName);
        return await _usersCollection.Find(filter).ToListAsync(cancellationToken);
    }

    #endregion

    #region IUserLoginStore

    public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(login);

        if (!user.Logins.Any(l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey))
        {
            user.Logins.Add(new IdentityUserLogin<string>
            {
                LoginProvider = login.LoginProvider,
                ProviderKey = login.ProviderKey,
                ProviderDisplayName = login.ProviderDisplayName
            });
        }
        return Task.CompletedTask;
    }

    public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(loginProvider);
        ArgumentException.ThrowIfNullOrEmpty(providerKey);

        user.Logins.RemoveAll(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
        return Task.CompletedTask;
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IList<UserLoginInfo> logins = user.Logins
            .Select(l => new UserLoginInfo(l.LoginProvider!, l.ProviderKey!, l.ProviderDisplayName))
            .ToList();
        return Task.FromResult(logins);
    }

    public async Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(loginProvider);
        ArgumentException.ThrowIfNullOrEmpty(providerKey);
        cancellationToken.ThrowIfCancellationRequested();

        var filter = Builders<TUser>.Filter.And(
            Builders<TUser>.Filter.ElemMatch(u => u.Logins, l => l.LoginProvider == loginProvider),
            Builders<TUser>.Filter.ElemMatch(u => u.Logins, l => l.ProviderKey == providerKey)
        );

        return await _usersCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, GetType().Name);
    }

    #endregion
}
