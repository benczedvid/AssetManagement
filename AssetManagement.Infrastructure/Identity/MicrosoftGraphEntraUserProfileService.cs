using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Common.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;

namespace AssetManagement.Infrastructure.Identity;

public sealed class MicrosoftGraphEntraUserProfileService
    : IEntraUserProfileService
{
    private readonly GraphServiceClient _graphServiceClient;

    public MicrosoftGraphEntraUserProfileService(
        GraphServiceClient graphServiceClient)
    {
        ArgumentNullException.ThrowIfNull(graphServiceClient);

        _graphServiceClient = graphServiceClient;
    }

    public async Task<EntraUserProfile?> GetByObjectIdAsync(
        Guid entraObjectId,
        CancellationToken cancellationToken = default)
    {
        if (entraObjectId == Guid.Empty)
        {
            throw new ArgumentException(
                "The Entra Object ID cannot be empty.",
                nameof(entraObjectId));
        }

        try
        {
            var entraUser = await _graphServiceClient
                .Users[entraObjectId.ToString()]
                .GetAsync(
                    requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select =
                        [
                            "id",
                            "employeeId",
                            "givenName",
                            "surname",
                            "displayName",
                            "mail",
                            "department",
                            "jobTitle",
                            "mobilePhone",
                            "accountEnabled"
                        ];
                    },
                    cancellationToken);

            if (entraUser is null)
            {
                return null;
            }

            return Map(entraUser);
        }
        catch (ApiException exception)
            when (exception.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    private static EntraUserProfile Map(User entraUser)
    {
        if (!Guid.TryParse(
                entraUser.Id,
                out var entraObjectId))
        {
            throw new InvalidOperationException(
                "Microsoft Graph returned an invalid Entra Object ID.");
        }

        if (string.IsNullOrWhiteSpace(entraUser.DisplayName))
        {
            throw new InvalidOperationException(
                "Microsoft Graph returned a user without a display name.");
        }

        return new EntraUserProfile(
            EntraObjectId: entraObjectId,
            EmployeeNumber: NormalizeOptional(entraUser.EmployeeId),
            FirstName: NormalizeOptional(entraUser.GivenName),
            LastName: NormalizeOptional(entraUser.Surname),
            DisplayName: entraUser.DisplayName.Trim(),
            Mail: NormalizeOptional(entraUser.Mail),
            Department: NormalizeOptional(entraUser.Department),
            JobTitle: NormalizeOptional(entraUser.JobTitle),
            MobilePhone: NormalizeOptional(entraUser.MobilePhone),
            IsAccountEnabled: entraUser.AccountEnabled ?? false
            );
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}