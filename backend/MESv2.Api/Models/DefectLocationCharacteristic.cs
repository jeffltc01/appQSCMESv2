namespace MESv2.Api.Models;

public class DefectLocationCharacteristic
{
    public Guid Id { get; set; }
    public Guid DefectLocationId { get; set; }
    public Guid CharacteristicId { get; set; }

    public DefectLocation DefectLocation { get; set; } = null!;
    public Characteristic Characteristic { get; set; } = null!;
}
