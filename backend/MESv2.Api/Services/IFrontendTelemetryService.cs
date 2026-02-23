using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IFrontendTelemetryService
{
    Task IngestAsync(FrontendTelemetryIngestDto dto, CancellationToken ct);

    Task<FrontendTelemetryPageDto> GetEventsAsync(
        string? category,
        string? source,
        string? severity,
        Guid? userId,
        Guid? workCenterId,
        DateTime? from,
        DateTime? to,
        bool reactRuntimeOnly,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<FrontendTelemetryFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct);
    Task<FrontendTelemetryCountDto> GetCountAsync(long warningThreshold, CancellationToken ct);
    Task<FrontendTelemetryArchiveResultDto> ArchiveOldestAsync(int keepRows, CancellationToken ct);
}
