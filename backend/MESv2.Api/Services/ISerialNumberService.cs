using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface ISerialNumberService
{
    Task<SerialNumberContextDto?> GetContextAsync(string serial, CancellationToken cancellationToken = default);
    Task<SerialNumberLookupDto?> GetLookupAsync(string serial, CancellationToken cancellationToken = default);
}
