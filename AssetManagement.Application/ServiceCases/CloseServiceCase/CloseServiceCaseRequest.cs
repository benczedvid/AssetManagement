namespace AssetManagement.Application.ServiceCases.CloseServiceCase;

/// <summary>
/// Represents a request to close a resolved service case.
/// </summary>
/// <param name="Comment">
/// An optional comment describing the closure of the service case.
/// If provided, the comment must not be empty or whitespace and must not
/// exceed 2000 characters.
/// </param>
public sealed record CloseServiceCaseRequest(
    string? Comment);