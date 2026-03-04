using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class SerialProcessingBlockedException : InvalidOperationException
{
    public SerialProcessingBlockedException(string serialNumber, SerialProcessingBlockResultDto blockResult)
        : base($"Serial '{serialNumber}' is blocked for processing.")
    {
        SerialNumber = serialNumber;
        BlockResult = blockResult;
    }

    public string SerialNumber { get; }
    public SerialProcessingBlockResultDto BlockResult { get; }
}
