using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class AnnotationMapper
{
    public static Annotation? Map(object row, HashSet<Guid> productionRecordIds, MigrationLogger log)
    {
        var d = (IDictionary<string, object>)row;

        string recordType = (S(d, "RecordType") ?? "").Trim();
        var recordUid = Gn(d, "RecordUID");

        if (!recordUid.HasValue)
        {
            log.Warn($"Annotation {G(d, "Id")}: No RecordUID. Skipping.");
            return null;
        }

        if (!productionRecordIds.Contains(recordUid.Value))
        {
            log.Warn($"Annotation {G(d, "Id")}: RecordType='{recordType}' RecordUID='{recordUid}' cannot be resolved to a ProductionRecord. Skipping.");
            return null;
        }

        string? flagStatus = S(d, "FlagStatus");
        var status = string.IsNullOrEmpty(flagStatus)
            || flagStatus.Equals("None", StringComparison.OrdinalIgnoreCase)
            || flagStatus.Equals("Resolved", StringComparison.OrdinalIgnoreCase)
            ? AnnotationStatus.Closed
            : AnnotationStatus.Open;

        return new Annotation
        {
            Id = G(d, "Id"),
            ProductionRecordId = recordUid.Value,
            AnnotationTypeId = G(d, "AnnotationTypeUID"),
            Status = status,
            Notes = S(d, "AnnotationNotes"),
            InitiatedByUserId = G(d, "InitiatedById"),
            ResolvedByUserId = Gn(d, "ResolvedByUID"),
            ResolvedNotes = S(d, "ResolutionNotes"),
            CreatedAt = Dt(d, "AnnotationDateTime") ?? DateTime.UtcNow
        };
    }

    private static Guid G(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is Guid g ? g : Guid.Empty;
    private static Guid? Gn(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is Guid g && g != Guid.Empty ? g : null;
    private static string? S(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v != null && v is not DBNull ? v.ToString() : null;
    private static DateTime? Dt(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is DateTime dt ? dt : null;
}
