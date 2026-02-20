namespace MESv2.Api.DTOs;

public class LoginConfigDto
{
    public bool RequiresPin { get; set; }
    public Guid DefaultSiteId { get; set; }
    public bool AllowSiteSelection { get; set; }
    public bool IsWelder { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string? Pin { get; set; }
    public Guid SiteId { get; set; }
    public bool IsWelder { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal RoleTier { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid DefaultSiteId { get; set; }
    public bool IsCertifiedWelder { get; set; }
    public string PlantCode { get; set; } = string.Empty;
    public string PlantTimeZoneId { get; set; } = string.Empty;
}

public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}
