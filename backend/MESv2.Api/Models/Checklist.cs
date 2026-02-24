namespace MESv2.Api.Models;

public static class ChecklistScopeLevels
{
    public const string PlantWorkCenter = "PlantWorkCenter";
    public const string SiteDefault = "SiteDefault";
    public const string GlobalDefault = "GlobalDefault";
}

public static class ChecklistResponseModes
{
    public const string PassFail = "PF";
    public const string PassFailNa = "PFNA";
}

public static class ChecklistQuestionResponseTypes
{
    public const string PassFail = "PassFail";
    public const string Text = "Text";
    public const string Select = "Select";
    public const string Date = "Date";
}

public static class ChecklistEntryStatuses
{
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
}

public class ChecklistTemplate
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChecklistType { get; set; } = string.Empty;
    public string ScopeLevel { get; set; } = ChecklistScopeLevels.PlantWorkCenter;
    public Guid? SiteId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? ProductionLineId { get; set; }
    public int VersionNo { get; set; } = 1;
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public string ResponseMode { get; set; } = ChecklistResponseModes.PassFailNa;
    public bool RequireFailNote { get; set; }
    public bool IsSafetyProfile { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Plant? Site { get; set; }
    public WorkCenter? WorkCenter { get; set; }
    public ProductionLine? ProductionLine { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ChecklistTemplateItem> Items { get; set; } = new List<ChecklistTemplateItem>();
}

public class ChecklistTemplateItem
{
    public Guid Id { get; set; }
    public Guid ChecklistTemplateId { get; set; }
    public int SortOrder { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public string? ResponseMode { get; set; }
    public string ResponseType { get; set; } = ChecklistQuestionResponseTypes.PassFail;
    public string? ResponseOptionsJson { get; set; }
    public string? HelpText { get; set; }
    public bool RequireFailNote { get; set; }

    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
}

public class ChecklistEntry
{
    public Guid Id { get; set; }
    public Guid ChecklistTemplateId { get; set; }
    public string ChecklistType { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? ProductionLineId { get; set; }
    public Guid OperatorUserId { get; set; }
    public string Status { get; set; } = ChecklistEntryStatuses.InProgress;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string ResolvedFromScope { get; set; } = string.Empty;
    public string ResolvedTemplateCode { get; set; } = string.Empty;
    public int ResolvedTemplateVersionNo { get; set; }

    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
    public Plant Site { get; set; } = null!;
    public WorkCenter WorkCenter { get; set; } = null!;
    public ProductionLine? ProductionLine { get; set; }
    public User OperatorUser { get; set; } = null!;
    public ICollection<ChecklistEntryItemResponse> Responses { get; set; } = new List<ChecklistEntryItemResponse>();
}

public class ChecklistEntryItemResponse
{
    public Guid Id { get; set; }
    public Guid ChecklistEntryId { get; set; }
    public Guid ChecklistTemplateItemId { get; set; }
    public string ResponseValue { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime RespondedAtUtc { get; set; } = DateTime.UtcNow;

    public ChecklistEntry ChecklistEntry { get; set; } = null!;
    public ChecklistTemplateItem ChecklistTemplateItem { get; set; } = null!;
}
