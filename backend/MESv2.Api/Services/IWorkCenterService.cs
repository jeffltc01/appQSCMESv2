using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IWorkCenterService
{
    Task<IReadOnlyList<WorkCenterDto>> GetWorkCentersAsync(string siteCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WelderDto>> GetWeldersAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<WelderDto?> AddWelderAsync(Guid wcId, string empNo, CancellationToken cancellationToken = default);
    Task<bool> RemoveWelderAsync(Guid wcId, Guid userId, CancellationToken cancellationToken = default);
    Task<WCHistoryDto> GetHistoryAsync(Guid wcId, string date, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialQueueItemDto>> GetMaterialQueueAsync(Guid wcId, string? type, CancellationToken cancellationToken = default);
    Task<QueueAdvanceResponseDto?> AdvanceQueueAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task ReportFaultAsync(Guid wcId, string description, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DefectCodeDto>> GetDefectCodesAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DefectLocationDto>> GetDefectLocationsAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CharacteristicDto>> GetCharacteristicsAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<KanbanCardLookupDto?> GetCardLookupAsync(string cardId, CancellationToken cancellationToken = default);
}
