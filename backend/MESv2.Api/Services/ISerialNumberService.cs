using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface ISerialNumberService
{
    Task<SerialNumberContextDto?> GetContextAsync(string serial, CancellationToken cancellationToken = default);
}
