using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IDowntimeService
{
    // Reason Categories
    Task<IReadOnlyList<DowntimeReasonCategoryDto>> GetCategoriesAsync(Guid plantId, CancellationToken cancellationToken = default);
    Task<DowntimeReasonCategoryDto> CreateCategoryAsync(CreateDowntimeReasonCategoryDto dto, CancellationToken cancellationToken = default);
    Task<DowntimeReasonCategoryDto?> UpdateCategoryAsync(Guid id, UpdateDowntimeReasonCategoryDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    // Reasons
    Task<IReadOnlyList<DowntimeReasonDto>> GetReasonsAsync(Guid plantId, CancellationToken cancellationToken = default);
    Task<DowntimeReasonDto> CreateReasonAsync(CreateDowntimeReasonDto dto, CancellationToken cancellationToken = default);
    Task<DowntimeReasonDto?> UpdateReasonAsync(Guid id, UpdateDowntimeReasonDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteReasonAsync(Guid id, CancellationToken cancellationToken = default);

    // Downtime Config (per WC/PL)
    Task<DowntimeConfigDto?> GetDowntimeConfigAsync(Guid wcId, Guid productionLineId, CancellationToken cancellationToken = default);
    Task<DowntimeConfigDto?> UpdateDowntimeConfigAsync(Guid wcId, Guid productionLineId, UpdateDowntimeConfigDto dto, CancellationToken cancellationToken = default);
    Task<bool> SetDowntimeReasonsAsync(Guid wcId, Guid productionLineId, List<Guid> reasonIds, CancellationToken cancellationToken = default);

    // Downtime Events
    Task<DowntimeEventDto> CreateDowntimeEventAsync(CreateDowntimeEventDto dto, Guid initiatedByUserId, CancellationToken cancellationToken = default);
}
