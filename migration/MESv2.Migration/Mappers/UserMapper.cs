using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class UserMapper
{
    // v1 UserType: int, but semantics unclear. v2 has RoleTier/RoleName + UserType enum.
    // Default mapping: all v1 users become Operator (tier 6.0) unless we can infer otherwise.
    // v1 stores plain Pin; v2 stores PinHash via BCrypt.
    private static int _emptyEmpNoCounter = 0;

    public static User? Map(dynamic row, Dictionary<string, Guid> plantsByCode, Dictionary<Guid, string> plantsById)
    {
        object fnRaw = row.FirstName;
        object lnRaw = row.LastName;
        string firstName = fnRaw != null ? fnRaw.ToString()! : "";
        string lastName = lnRaw != null ? lnRaw.ToString()! : "";

        object disRaw = row.IsDisabled;
        bool isDisabled = false;
        if (disRaw is int di) isDisabled = di != 0;
        else if (disRaw is bool db) isDisabled = db;

        Guid defaultSiteId = Guid.Empty;
        object dsi = row.DefaultSiteId;
        if (dsi is Guid g && g != Guid.Empty)
            defaultSiteId = g;
        else if (dsi is string sc && plantsByCode.TryGetValue(sc, out var pid))
            defaultSiteId = pid;

        if (defaultSiteId == Guid.Empty || !plantsById.ContainsKey(defaultSiteId))
            defaultSiteId = plantsByCode.Values.FirstOrDefault();

        object utRaw = row.UserType;
        int v1UserType = 0;
        if (utRaw is int ut) v1UserType = ut;
        else if (utRaw is short us) v1UserType = us;
        var (roleTier, roleName) = MapUserTypeToRole(v1UserType);

        string? pinHash = null;
        string? plainPin = row.Pin as string;
        if (!string.IsNullOrEmpty(plainPin))
            pinHash = BCrypt.Net.BCrypt.HashPassword(plainPin);

        object empRaw = row.EmployeeNo;
        string empNo = empRaw != null ? empRaw.ToString()! : "";
        if (string.IsNullOrWhiteSpace(empNo))
            empNo = $"V1-{Interlocked.Increment(ref _emptyEmpNoCounter):D4}";

        object rpRaw = row.RequirePinForLogin;
        bool requirePin = false;
        if (rpRaw is bool rpb) requirePin = rpb;
        else if (rpRaw is int rpi) requirePin = rpi != 0;

        return new User
        {
            Id = (Guid)row.Id,
            EmployeeNumber = empNo,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{firstName} {lastName}".Trim(),
            RoleTier = roleTier,
            RoleName = roleName,
            DefaultSiteId = defaultSiteId,
            IsCertifiedWelder = false,
            RequirePinForLogin = requirePin,
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
