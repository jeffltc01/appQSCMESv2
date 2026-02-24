namespace MESv2.Api.Services;

public class DemoDataAdminOptions
{
    public bool Enabled { get; set; }
    public List<string> AllowedEnvironments { get; set; } = new();
    public string? RequiredConnectionStringContains { get; set; }
}
