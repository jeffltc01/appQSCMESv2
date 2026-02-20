using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IHydroService
{
    Task<HydroRecordResponseDto> CreateAsync(CreateHydroRecordDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DefectLocationDto>> GetLocationsByCharacteristicAsync(Guid characteristicId, CancellationToken cancellationToken = default);
}
