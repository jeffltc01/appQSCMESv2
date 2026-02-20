namespace MESv2.Api.Models;

public class AnnotationType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public bool RequiresResolution { get; set; }
    public bool OperatorCanCreate { get; set; }
    public string? DisplayColor { get; set; }

    public ICollection<Annotation> Annotations { get; set; } = new List<Annotation>();
}
