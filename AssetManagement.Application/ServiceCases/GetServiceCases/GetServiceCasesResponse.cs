namespace AssetManagement.Application.ServiceCases.GetServiceCases;

/// <summary>
/// Represents the collection of service cases accessible to the current user.
/// </summary>
/// <param name="Items">
/// The list of service cases accessible to the current user based on their permissions and store access scope.
/// </param>
public sealed record GetServiceCasesResponse(
    IReadOnlyList<ServiceCaseListItemResponse> Items);