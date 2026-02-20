using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class AnnotationTypeMapper
{
    public static AnnotationType? Map(dynamic row)
    {
        return new AnnotationType
        {
            Id = (Guid)row.Id,
            Name = (string)(row.Name ?? ""),
            Abbreviation = (string?)row.Abbreviation,
            RequiresResolution = (bool?)row.RequiresResolution ?? false,
            OperatorCanCreate = (bool?)row.OperatorAllowed ?? false,
            DisplayColor = (string?)row.AnnotationColor
        };
    }
}
