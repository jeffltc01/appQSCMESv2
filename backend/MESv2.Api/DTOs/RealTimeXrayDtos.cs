using System.Text.Json.Serialization;

namespace MESv2.Api.DTOs;

public class XrayInspectionRequestDto
{
    [JsonPropertyName("serialNumber")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("inspectionResult")]
    public int InspectionResult { get; set; }

    [JsonPropertyName("siteCode")]
    public string SiteCode { get; set; } = string.Empty;

    [JsonPropertyName("userID")]
    public Guid UserID { get; set; }

    [JsonPropertyName("isTest")]
    public int IsTest { get; set; }

    [JsonPropertyName("defects")]
    public List<XrayDefectDto> Defects { get; set; } = new();
}

public class XrayDefectDto
{
    [JsonPropertyName("defectID")]
    public Guid DefectID { get; set; }

    [JsonPropertyName("locationDetails1")]
    public decimal LocationDetails1 { get; set; }

    [JsonPropertyName("locationDetails2")]
    public decimal LocationDetails2 { get; set; }

    [JsonPropertyName("locationDetailsCode")]
    public string LocationDetailsCode { get; set; } = string.Empty;
}

public class XrayInspectionResponseDto
{
    [JsonPropertyName("isSuccess")]
    public int IsSuccess { get; set; }

    [JsonPropertyName("errors")]
    public List<XrayErrorDto> Errors { get; set; } = new();
}

public class XrayErrorDto
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
