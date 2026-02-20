using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class PlantGearMapper
{
    /// <summary>
    /// V1 mesPlantGears are global (no site association). V2 requires PlantId.
    /// This mapper is called per (v1Gear, plantId) pair by the runner, which
    /// duplicates each gear across all plants.
    /// </summary>
    public static PlantGear Map(dynamic row, Guid plantId, Guid deterministicId)
    {
        return new PlantGear
        {
            Id = deterministicId,
            Name = (string)(row.GearName ?? ""),
            Level = (int)(row.Gear ?? 0),
            PlantId = plantId
        };
    }
}
