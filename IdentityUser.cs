using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.AspNetCore.Identity;

/// <summary>
/// Represents a user in the identity system with MongoDB storage
/// </summary>
public class IdentityUser : Microsoft.AspNetCore.Identity.IdentityUser<string>
{
    static IdentityUser()
    {
        // Configure BSON serialization to avoid conflict with base class Id property
        if (!BsonClassMap.IsClassMapRegistered(typeof(IdentityUser)))
        {
            BsonClassMap.RegisterClassMap<IdentityUser>(cm =>
            {
                // Don't auto-map to avoid base class Id conflict
                var idMemberMap = cm.MapMember(u => u.Id);
                idMemberMap.SetElementName("_id");
                cm.SetIdMember(idMemberMap);
                cm.MapMember(u => u.UserName);
                cm.MapMember(u => u.NormalizedUserName);
                cm.MapMember(u => u.Email);
                cm.MapMember(u => u.NormalizedEmail);
                cm.MapMember(u => u.EmailConfirmed);
                cm.MapMember(u => u.PasswordHash);
                cm.MapMember(u => u.SecurityStamp);
                cm.MapMember(u => u.PhoneNumber);
                cm.MapMember(u => u.PhoneNumberConfirmed);
                cm.MapMember(u => u.TwoFactorEnabled);
                cm.MapMember(u => u.LockoutEnd);
                cm.MapMember(u => u.LockoutEnabled);
                cm.MapMember(u => u.AccessFailedCount);
                cm.MapMember(u => u.Roles);
                cm.MapMember(u => u.Claims);
                cm.MapMember(u => u.Logins);
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    /// <summary>
    /// Gets or sets the unique identifier for the user in MongoDB
    /// </summary>
    public override string? Id { get; set; }

    /// <summary>
    /// Gets the list of roles for this user stored as embedded documents in MongoDB
    /// </summary>
    public List<string> Roles { get; set; }

    /// <summary>
    /// Gets the list of claims for this user stored as embedded documents in MongoDB
    /// </summary>
    public List<IdentityUserClaim<string>> Claims { get; set; }

    /// <summary>
    /// Gets the list of external logins for this user stored as embedded documents in MongoDB
    /// </summary>
    public List<IdentityUserLogin<string>> Logins { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityUser"/> class.
    /// </summary>
    public IdentityUser()
    {
        Id = ObjectId.GenerateNewId().ToString();
        Roles = new List<string>();
        Claims = new List<IdentityUserClaim<string>>();
        Logins = new List<IdentityUserLogin<string>>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityUser"/> class.
    /// </summary>
    /// <param name="userName">The username for the user.</param>
    public IdentityUser(string userName) : this()
    {
        UserName = userName;
        NormalizedUserName = userName?.ToUpperInvariant();
    }
}
