using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.DispatchToExternalService;

/// <summary>
/// Represents the result of successfully dispatching an asset to an external service provider.
/// </summary>
/// <param name="ServiceCaseId">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="ServiceCaseStatus">The current status of the service case.</param>
/// <param name="AssetStatus">The current status of the asset.</param>
/// <param name="VendorId">The unique identifier of the external service provider assigned to the service case.</param>
/// <param name="DispatchedAtUtc">The date and time when the asset was dispatched to the external service provider, in UTC.</param>
/// <param name="DispatchedByUserId">The unique identifier of the user who dispatched the asset.</param>
/// <param name="DispatchedByUserName">The display name of the user who dispatched the asset.</param>
public sealed record DispatchToExternalServiceResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    AssetStatus AssetStatus,
    Guid VendorId,
    DateTime DispatchedAtUtc,
    Guid DispatchedByUserId,
    string DispatchedByUserName
    );