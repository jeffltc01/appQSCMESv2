namespace MESv2.Api.Models;

public class TraceabilityLog
{
    public Guid Id { get; set; }
    public Guid? FromSerialNumberId { get; set; }
    public Guid? ToSerialNumberId { get; set; }
    public string? FromAlphaCode { get; set; }
    public string? ToAlphaCode { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public string? TankLocation { get; set; }
    public DateTime Timestamp { get; set; }
}
