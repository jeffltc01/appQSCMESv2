using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IWorkCenterService
{
    Task<IReadOnlyList<WorkCenterDto>> GetWorkCentersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WelderDto>> GetWeldersAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<WelderDto?> LookupWelderAsync(string empNo, CancellationToken cancellationToken = default);
    Task<WelderDto?> AddWelderAsync(Guid wcId, string empNo, CancellationToken cancellationToken = default);
    Task<bool> RemoveWelderAsync(Guid wcId, Guid userId, CancellationToken cancellationToken = default);
    Task<WCHistoryDto> GetHistoryAsync(Guid wcId, Guid plantId, string? date, int limit, Guid? assetId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialQueueItemDto>> GetMaterialQueueAsync(Guid wcId, string? type, CancellationToken cancellationToken = default);
    Task<QueueAdvanceResponseDto?> AdvanceQueueAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task ReportFaultAsync(Guid wcId, string description, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DefectCodeDto>> GetDefectCodesAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DefectLocationDto>> GetDefectLocationsAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CharacteristicDto>> GetCharacteristicsAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<KanbanCardLookupDto?> GetCardLookupAsync(string cardId, CancellationToken cancellationToken = default);
    Task<MaterialQueueItemDto> AddMaterialQueueItemAsync(Guid wcId, CreateMaterialQueueItemDto dto, CancellationToken cancellationToken = default);
    Task<MaterialQueueItemDto?> UpdateMaterialQueueItemAsync(Guid wcId, Guid itemId, UpdateMaterialQueueItemDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteMaterialQueueItemAsync(Guid wcId, Guid itemId, CancellationToken cancellationToken = default);
    Task<MaterialQueueItemDto> AddFitupQueueItemAsync(Guid wcId, CreateFitupQueueItemDto dto, CancellationToken cancellationToken = default);
    Task<MaterialQueueItemDto?> UpdateFitupQueueItemAsync(Guid wcId, Guid itemId, UpdateFitupQueueItemDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteFitupQueueItemAsync(Guid wcId, Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BarcodeCardDto>> GetBarcodeCardsAsync(Guid? plantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueueTransactionDto>> GetQueueTransactionsAsync(Guid wcId, int limit, Guid? plantId = null, CancellationToken cancellationToken = default);
}
