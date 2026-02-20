using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class UserMapper
{
    // v1 UserType: int, but semantics unclear. v2 has RoleTier/RoleName + UserType enum.
    // Default mapping: all v1 users become Operator (tier 6.0) unless we can infer otherwise.
    // v1 stores plain Pin; v2 stores PinHash via BCrypt.
    public static User? Map(dynamic row, Dictionary<string, Guid> plantsByCode, Dictionary<Guid, string> plantsById)
    {
        string firstName = (string)(row.FirstName ?? "");
        string lastName = (string)(row.LastName ?? "");
        bool isDisabled = ((int?)row.IsDisabled ?? 0) != 0;

        // Resolve DefaultSiteId: v1 stores it as a GUID FK to mesSite
        Guid defaultSiteId = (Guid?)row.DefaultSiteId ?? Guid.Empty;

        // Map v1 UserType int to v2 role tier
        int v1UserType = (int?)row.UserType ?? 0;
        var (roleTier, roleName) = MapUserTypeToRole(v1UserType);

        // Hash the plain-text PIN if present
        string? pinHash = null;
        string? plainPin = row.Pin as string;
        if (!string.IsNullOrEmpty(plainPin))
            pinHash = BCrypt.Net.BCrypt.HashPassword(plainPin);

        return new User
        {
            Id = (Guid)row.Id,
            EmployeeNumber = (string)(row.EmployeeNo ?? ""),
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{firstName} {lastName}".Trim(),
            RoleTier = roleTier,
            RoleName = roleName,
            DefaultSiteId = defaultSiteId,
            IsCertifiedWelder = false,
            RequirePinForLogin = (bool?)row.RequirePinForLogin ?? false,
            PinHash = pinHash,
            UserType = v1UserType == 1 ? UserType.AuthorizedInspector : UserType.Standard,
            IsActive = !isDisabled
        };
    }

    private static (decimal Tier, string Name) MapUserTypeToRole(int v1Type)
    {
        return v1Type switch
        {
            0 => (6.0m, "Operator"),
            1 => (5.5m, "Authorized Inspector"),
            _ => (6.0m, "Operator")
        };
    }
}
