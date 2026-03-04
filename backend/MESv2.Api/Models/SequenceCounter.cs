namespace MESv2.Api.Models;

public class SequenceCounter
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
}
