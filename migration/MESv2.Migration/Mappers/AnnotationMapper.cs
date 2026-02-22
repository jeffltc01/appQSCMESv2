using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class AnnotationMapper
{
    /// <summary>
    /// Maps v1 mesAnnotation (polymorphic RecordType+RecordUID) to v2 Annotation (explicit ProductionRecordId).
    /// Only rows whose RecordUID maps to an existing ProductionRecord are migrated.
    /// </summary>
    public static Annotation? Map(dynamic row, HashSet<Guid> productionRecordIds, MigrationLogger log)
    {
        string recordType = ((string?)row.RecordType ?? "").Trim();
        Guid? recordUid = (Guid?)row.RecordUID;

        Guid? productionRecordId = null;

        if (recordUid.HasValue)
        {
            if (recordType.Equals("ManufacturingLog", StringComparison.OrdinalIgnoreCase)
                && productionRecordIds.Contains(recordUid.Value))
            {
                productionRecordId = recordUid.Value;
            }
            else if (productionRecordIds.Contains(recordUid.Value))
            {
                productionRecordId = recordUid.Value;
            }
            else
            {
                log.Warn($"Annotation {row.Id}: RecordType='{recordType}' RecordUID='{recordUid}' cannot be resolved to a ProductionRecord. Skipping.");
                return null;
            }
        }
        else
        {
            log.Warn($"Annotation {row.Id}: No RecordUID. Skipping.");
            return null;
        }

        string? flagStatus = row.FlagStatus as string;
        var status = string.IsNullOrEmpty(flagStatus)
            || flagStatus.Equals("None", StringComparison.OrdinalIgnoreCase)
            || flagStatus.Equals("Resolved", StringComparison.OrdinalIgnoreCase)
            ? AnnotationStatus.Closed
            : AnnotationStatus.Open;

        return new Annotation
        {
            Id = (Guid)row.Id,
            ProductionRecordId = productionRecordId.Value,
            AnnotationTypeId = (Guid)row.AnnotationTypeUID,
            Status = status,
            Notes = (string?)row.AnnotationNotes,
            InitiatedByUserId = (Guid?)row.InitiatedById ?? Guid.Empty,
            ResolvedByUserId = (Guid?)row.ResolvedByUID,
            ResolvedNotes = (string?)row.ResolutionNotes,
            CreatedAt = (DateTime?)row.AnnotationDateTime ?? DateTime.UtcNow
        };
    }
}
