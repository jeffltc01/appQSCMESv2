namespace MESv2.Api.DTOs;

public class LimbleStatusDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class LimbleTaskDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Priority { get; set; }
    public int? StatusId { get; set; }
    public string? StatusName { get; set; }
    public long? DueDate { get; set; }
    public long? CreatedDate { get; set; }
    public string? Meta1 { get; set; }
}

public class CreateLimbleWorkRequestInputDto
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public long? RequestedDueDate { get; set; }
}

public class CreateLimbleWorkRequestDto
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public long? RequestedDueDate { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
