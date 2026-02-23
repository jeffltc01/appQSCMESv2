using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IAdminWorkCenterService
{
    Task<IReadOnlyList<AdminWorkCenterDto>> GetAllAdminAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminWorkCenterGroupDto>> GetAllGroupedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WorkCenterTypeDto>> GetWorkCenterTypesAsync(CancellationToken ct = default);
    Task<AdminWorkCenterGroupDto> CreateWorkCenterAsync(CreateWorkCenterDto dto, CancellationToken ct = default);
    Task<AdminWorkCenterGroupDto?> UpdateGroupAsync(Guid groupId, UpdateWorkCenterGroupDto dto, CancellationToken ct = default);
    Task<AdminWorkCenterDto?> UpdateConfigAsync(Guid id, UpdateWorkCenterConfigDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<AdminWorkCenterProductionLineDto>> GetProductionLineConfigsAsync(Guid wcId, CancellationToken ct = default);
    Task<WorkCenterProductionLineDto?> GetProductionLineConfigAsync(Guid wcId, Guid plId, CancellationToken ct = default);
    Task<AdminWorkCenterProductionLineDto> CreateProductionLineConfigAsync(Guid wcId, CreateWorkCenterProductionLineDto dto, CancellationToken ct = default);
    Task<AdminWorkCenterProductionLineDto?> UpdateProductionLineConfigAsync(Guid wcId, Guid plId, UpdateWorkCenterProductionLineDto dto, CancellationToken ct = default);
    Task<bool> DeleteProductionLineConfigAsync(Guid wcId, Guid plId, CancellationToken ct = default);
}
