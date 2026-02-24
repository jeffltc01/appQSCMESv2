namespace MESv2.Api.DTOs;

public class ChecklistTemplateItemDto
{
    public Guid? Id { get; set; }
    public int SortOrder { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public string? ResponseMode { get; set; }
    public string? ResponseType { get; set; }
    public List<string> ResponseOptions { get; set; } = [];
    public string? HelpText { get; set; }
    public bool RequireFailNote { get; set; }
}

public class ChecklistTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChecklistType { get; set; } = string.Empty;
    public string ScopeLevel { get; set; } = string.Empty;
    public Guid? SiteId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? ProductionLineId { get; set; }
    public int VersionNo { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; }
    public string ResponseMode { get; set; } = string.Empty;
    public bool RequireFailNote { get; set; }
    public bool IsSafetyProfile { get; set; }
    public List<ChecklistTemplateItemDto> Items { get; set; } = [];
}

public class UpsertChecklistTemplateRequestDto
{
    public Guid? Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChecklistType { get; set; } = string.Empty;
    public string ScopeLevel { get; set; } = string.Empty;
    public Guid? SiteId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? ProductionLineId { get; set; }
    public int VersionNo { get; set; } = 1;
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public string ResponseMode { get; set; } = string.Empty;
    public bool RequireFailNote { get; set; }
    public bool IsSafetyProfile { get; set; }
    public List<Guid> DeletedItemIds { get; set; } = [];
    public List<ChecklistTemplateItemDto> Items { get; set; } = [];
}

public class ResolveChecklistTemplateRequestDto
{
    public string ChecklistType { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? ProductionLineId { get; set; }
}

public class ChecklistResponseDto
{
    public Guid? Id { get; set; }
    public Guid ChecklistTemplateItemId { get; set; }
    public string ResponseValue { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CreateChecklistEntryRequestDto
{
    public string ChecklistType { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? ProductionLineId { get; set; }
    public Guid OperatorUserId { get; set; }
}

public class SubmitChecklistResponsesRequestDto
{
    public List<ChecklistResponseDto> Responses { get; set; } = [];
}

public class ChecklistEntryDto
{
    public Guid Id { get; set; }
    public Guid ChecklistTemplateId { get; set; }
    public string ChecklistType { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? ProductionLineId { get; set; }
    public Guid OperatorUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string ResolvedFromScope { get; set; } = string.Empty;
    public string ResolvedTemplateCode { get; set; } = string.Empty;
    public int ResolvedTemplateVersionNo { get; set; }
    public List<ChecklistResponseDto> Responses { get; set; } = [];
}
