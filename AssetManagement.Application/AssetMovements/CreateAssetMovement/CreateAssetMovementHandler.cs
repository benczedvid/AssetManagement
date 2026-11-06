using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Domain.Entities.AssetMovements;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Employees;

namespace AssetManagement.Application.AssetMovements.CreateAssetMovement
{
    public sealed class CreateAssetMovementHandler
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetMovementRepository _assetMovementRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public CreateAssetMovementHandler(IAssetMovementRepository assetMovementRepository, IAssetRepository assetRepository, IEmployeeRepository employeeRepository)
        {
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(assetMovementRepository);
            ArgumentNullException.ThrowIfNull(employeeRepository);

            _assetRepository = assetRepository;
            _assetMovementRepository = assetMovementRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task<CreateAssetMovementResponse> HandleAsync(CreateAssetMovementRequest request, CancellationToken cancellationToken) {
            ArgumentNullException.ThrowIfNull(request);

            var assignedAsset = await _assetRepository.GetByRfidTagIdAsync(request.RfidTagId, cancellationToken) ?? throw new AssetNotFoundException(request.RfidTagId);
            var assignedEmployee = await _employeeRepository.GetByEmployeeNumberAsync(request.EmployeeNumber, cancellationToken) ?? throw new EmployeeNotFoundException(request.EmployeeNumber);

            if (assignedAsset.AssetType != AssetType.PDT) {
                throw new InvalidOperationException("Only PDT assets can be checked out or returned.");
            }
            var movementType = assignedAsset.AssetStatus switch
            {
                AssetStatus.In_Store => AssetMovementType.Checkout,
                AssetStatus.Used_In_Store => AssetMovementType.Return,

                _ => throw new InvalidOperationException($"The asset cannot be moved while its status is {assignedAsset.AssetStatus}.")
            };

            switch (movementType)
            {
                case AssetMovementType.Checkout:
                    assignedAsset.Checkout(assignedEmployee.EmployeeNumber);
                    break;

                case AssetMovementType.Return:
                    assignedAsset.Return(assignedEmployee.EmployeeNumber);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(movementType),
                        movementType,
                        "The asset movement type is not supported.");
            }

            var newAssetMovement = AssetMovement.Create(
                assetId: assignedAsset.Id,
                storeId: assignedAsset.AssignedStoreId,
                employeeNumber: assignedEmployee.EmployeeNumber,
                type: movementType
                );

            await _assetMovementRepository.AddAsync(newAssetMovement, cancellationToken);
            await _assetMovementRepository.SaveChangesAsync(cancellationToken);

            
            return new CreateAssetMovementResponse(
                Id: newAssetMovement.Id,
                AssetId: newAssetMovement.AssetId,
                StoreId: newAssetMovement.StoreId,
                SerialNumber: assignedAsset.SerialNumber,
                EmployeeNumber: newAssetMovement.EmployeeNumber,
                Type: newAssetMovement.Type,
                CreatedAtUtc: newAssetMovement.CreatedAtUtc
                );
        }
    }
}
