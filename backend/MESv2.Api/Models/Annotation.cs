namespace MESv2.Api.Models;

public class Annotation
{
    public Guid Id { get; set; }
    public Guid ProductionRecordId { get; set; }
    public Guid AnnotationTypeId { get; set; }
    public bool Flag { get; set; }
    public string? Notes { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolvedNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    public ProductionRecord ProductionRecord { get; set; } = null!;
    public AnnotationType AnnotationType { get; set; } = null!;
    public User InitiatedByUser { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
}
