namespace AssetManagement.Application.ServiceCases.CompleteRepairAtHeadquarters;

/// <summary>
/// Represents a request to complete the repair of an asset at headquarters.
/// </summary>
/// <param name="Comment">
/// An optional comment describing the repair completion.
/// </param>
public sealed record CompleteRepairAtHeadquartersRequest(
    string? Comment);