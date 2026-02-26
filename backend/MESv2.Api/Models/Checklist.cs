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
    public const string Checkbox = "Checkbox";
    public const string Datetime = "Datetime";
    public const string Number = "Number";
    public const string Image = "Image";
    public const string Dimension = "Dimension";
    public const string Score = "Score";
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
    public Guid OwnerUserId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Plant? Site { get; set; }
    public WorkCenter? WorkCenter { get; set; }
    public ProductionLine? ProductionLine { get; set; }
    public User OwnerUser { get; set; } = null!;
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
    public string? Section { get; set; }
    public string? ResponseMode { get; set; }
    public string ResponseType { get; set; } = ChecklistQuestionResponseTypes.Checkbox;
    public string? ResponseOptionsJson { get; set; }
    public Guid? ScoreTypeId { get; set; }
    public decimal? DimensionTarget { get; set; }
    public decimal? DimensionUpperLimit { get; set; }
    public decimal? DimensionLowerLimit { get; set; }
    public string? DimensionUnitOfMeasure { get; set; }
    public string? HelpText { get; set; }
    public bool RequireFailNote { get; set; }

    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
    public ScoreType? ScoreType { get; set; }
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

public class ScoreType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedByUserId { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }

    public User CreatedByUser { get; set; } = null!;
    public User? ModifiedByUser { get; set; }
    public ICollection<ScoreTypeValue> Values { get; set; } = new List<ScoreTypeValue>();
}

public class ScoreTypeValue
{
    public Guid Id { get; set; }
    public Guid ScoreTypeId { get; set; }
    public decimal Score { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ScoreType ScoreType { get; set; } = null!;
}
