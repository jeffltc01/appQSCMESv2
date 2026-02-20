namespace MESv2.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal RoleTier { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid DefaultSiteId { get; set; }
    public bool IsCertifiedWelder { get; set; }
    public bool RequirePinForLogin { get; set; }
    public string? PinHash { get; set; }

    public Plant DefaultSite { get; set; } = null!;
}
