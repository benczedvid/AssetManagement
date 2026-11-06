using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.DisposeServiceCaseAsset;

/// <summary>
/// Represents the result of disposing of an asset through
/// its service case.
/// </summary>
/// <param name="ServiceCaseId">
/// The unique identifier of the service case.
/// </param>
/// <param name="CaseNumber">
/// The human-readable service case number.
/// </param>
/// <param name="AssetId">
/// The unique identifier of the disposed asset.
/// </param>
/// <param name="ServiceCaseStatus">
/// The service case status after asset disposal.
/// The service case is resolved but is not automatically closed.
/// </param>
/// <param name="AssetStatus">
/// The asset status after disposal.
/// </param>
/// <param name="Reason">
/// The validated and trimmed disposal reason recorded
/// in the service case event and asset history.
/// </param>
/// <param name="DisposedAtUtc">
/// The server-generated UTC timestamp of the disposal.
/// </param>
/// <param name="DisposedByUserId">
/// The internal application user identifier of the administrator
/// who performed the disposal.
/// </param>
/// <param name="DisposedByUserName">
/// The name of the administrator who performed the disposal.
/// </param>
/// <remarks>
/// Successful disposal changes the asset status to Disposed
/// and the service case status to Resolved.
///
/// Closing the service case remains a separate operation.
/// Existing store and vendor assignments are preserved.
/// </remarks>
public sealed record DisposeServiceCaseAssetResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    AssetStatus AssetStatus,
    string Reason,
    DateTime DisposedAtUtc,
    Guid DisposedByUserId,
    string DisposedByUserName);