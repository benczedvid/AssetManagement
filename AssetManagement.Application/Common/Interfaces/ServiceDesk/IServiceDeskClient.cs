using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Common.Interfaces.ServiceDesk;

public interface IServiceDeskClient
{
    Task<string> CreateRequestAsync(string subject, ApplicationUser requester, string description, AssetType assetType, CancellationToken cancellationToken = default);
    Task CloseRequestAsync(string serviceDeskRequestId, CancellationToken cancellationToken = default);
    Task<string> CreateRequestNoteAsync(string serviceDeskRequestId, string description, CancellationToken cancellationToken = default);
}