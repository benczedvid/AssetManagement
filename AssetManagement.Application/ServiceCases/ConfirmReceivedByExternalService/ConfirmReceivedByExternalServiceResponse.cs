using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.ConfirmReceivedByExternalService;

/// <summary>
/// Represents the result of successfully confirming that an asset was received by the external service provider.
/// </summary>
/// <param name="ServiceCaseId">The unique identifier of the service case.</param>
/// <param name="CaseNumber">The unique case number of the service case.</param>
/// <param name="AssetId">The unique identifier of the asset associated with the service case.</param>
/// <param name="ServiceCaseStatus">The current status of the service case.</param>
/// <param name="AssetStatus">The current status of the asset.</param>
/// <param name="VendorId">The unique identifier of the external service provider assigned to the service case.</param>
/// <param name="ConfirmedAtUtc">The date and time when the receipt by the external service provider was confirmed, in UTC.</param>
/// <param name="ConfirmedByUserId">The unique identifier of the user who confirmed the receipt.</param>
/// <param name="ConfirmedByUserName">The display name of the user who confirmed the receipt.</param>
public sealed record ConfirmReceivedByExternalServiceResponse(
    Guid ServiceCaseId,
    string CaseNumber,
    Guid AssetId,
    ServiceCaseStatus ServiceCaseStatus,
    AssetStatus AssetStatus,
    Guid VendorId,
    DateTime ConfirmedAtUtc,
    Guid ConfirmedByUserId,
    string ConfirmedByUserName);