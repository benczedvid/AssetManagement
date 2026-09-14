using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeAssetRepository : IAssetRepository
    {
        private readonly List<Asset> _assets = [];

        public IReadOnlyList<Asset> Assets => _assets.AsReadOnly();
        public int AddCallCount { get; private set; }
        public int GetAllCallCount { get; private set; }
        public int GetAllByStoreIdCallCount { get; private set; }
        public int GetByIdCallCount { get; private set; }
        public int GetByIdAndStoreIdCallCount { get; private set; }
        public int GetByAssignedUserIdCallCount { get; private set; }
        public int GetByRfidTagIdCallCount { get; private set; }
        public int GetBySerialNumberCallCount { get; private set; }
        public int GetForUpdateByIdCallCount { get; private set; }
        public int GetForUpdateBySerialNumberCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public Guid? LastRequestedAssetId { get; private set; }
        public Guid? LastRequestedStoreId { get; private set; }
        public Guid? LastRequestedAssignedUserId { get; private set; }
        public string? LastRequestedRfidTagId { get; private set; }
        public string? LastRequestedSerialNumber { get; private set; }
        public CancellationToken LastAddCancellationToken { get; private set; }
        public CancellationToken LastGetByIdCancellationToken { get; private set; }
        public CancellationToken LastGetByIdAndStoreIdCancellationToken { get; private set; }
        public CancellationToken LastGetByAssignedUserIdCancellationToken { get; private set; }
        public CancellationToken LastGetBySerialNumberCancellationToken { get; private set; }
        public CancellationToken LastSaveChangesCancellationToken { get; private set; }
        public CancellationToken LastGetAllCancellationToken { get; private set; }
        public CancellationToken LastGetForUpdateCancellationToken { get; private set; }
        public CancellationToken LastGetForUpdateByIdCancellationToken { get; private set; }
        public CancellationToken LastGetForUpdateBySerialNumberCancellationToken { get; private set; }
        public CancellationToken LastGetAllByStoreIdCancellationToken { get; private set; }
        public CancellationToken LastGetByRfidTagIdCancellationToken { get; private set; }

        public Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(asset);

            AddCallCount++;
            LastAddCancellationToken = cancellationToken;
            _assets.Add(asset);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;
            LastGetAllCancellationToken = cancellationToken;

            IReadOnlyList<Asset> assets = [.. _assets];

            return Task.FromResult(assets);
        }

        public Task<IReadOnlyList<Asset>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            GetAllByStoreIdCallCount++;
            LastRequestedStoreId = storeId;
            LastGetAllByStoreIdCancellationToken = cancellationToken;

            IReadOnlyList<Asset> assets =[.. _assets.Where(asset => asset.AssignedStoreId == storeId)];

            return Task.FromResult(assets);
        }

        public Task<IReadOnlyList<Asset>>GetByAssignedUserIdAsync(Guid assignedUserId, CancellationToken cancellationToken = default)
        {
            GetByAssignedUserIdCallCount++;
            LastRequestedAssignedUserId = assignedUserId;
            LastGetByAssignedUserIdCancellationToken = cancellationToken;

            IReadOnlyList<Asset> assets = [.. _assets.Where(asset => asset.AssignedUserId == assignedUserId)];

            return Task.FromResult(assets);
        }

        public Task<Asset?> GetByIdAndStoreIdAsync(Guid id, Guid storeId, CancellationToken cancellationToken = default)
        {
            GetByIdAndStoreIdCallCount++;
            LastRequestedAssetId = id;
            LastRequestedStoreId = storeId;
            LastGetByIdAndStoreIdCancellationToken = cancellationToken;

            var asset = _assets.SingleOrDefault(currentAsset => currentAsset.Id == id && currentAsset.AssignedStoreId == storeId);

            return Task.FromResult(asset);
        }

        public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            LastRequestedAssetId = id;
            LastGetByIdCancellationToken = cancellationToken;

            var asset = _assets.SingleOrDefault(currentAsset => currentAsset.Id == id);

            return Task.FromResult(asset);
        }

        public Task<Asset?> GetByRfidTagIdAsync(string rfidTagId, CancellationToken cancellationToken = default)
        {
            GetByRfidTagIdCallCount++;
            LastRequestedRfidTagId = rfidTagId;
            LastGetByRfidTagIdCancellationToken = cancellationToken;

            var asset = _assets.SingleOrDefault(currentAsset => currentAsset.RfidTagId == rfidTagId);

            return Task.FromResult(asset);
        }

        public Task<Asset?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            GetBySerialNumberCallCount++;
            LastRequestedSerialNumber = serialNumber;
            LastGetBySerialNumberCancellationToken = cancellationToken;

            var asset = _assets.SingleOrDefault(currentAsset => currentAsset.SerialNumber == serialNumber);

            return Task.FromResult(asset);
        }

        public Task<Asset?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetForUpdateByIdCallCount++;
            LastRequestedAssetId = id;
            LastGetForUpdateCancellationToken = cancellationToken;
            LastGetForUpdateByIdCancellationToken = cancellationToken;

            var asset = _assets.SingleOrDefault(currentAsset => currentAsset.Id == id);

            return Task.FromResult(asset);
        }

        public Task<Asset?> GetForUpdateBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            GetForUpdateBySerialNumberCallCount++;
            LastRequestedSerialNumber = serialNumber;
            LastGetForUpdateCancellationToken = cancellationToken;
            LastGetForUpdateBySerialNumberCancellationToken = cancellationToken;

            var asset = _assets.SingleOrDefault(currentAsset => currentAsset.SerialNumber == serialNumber);

            return Task.FromResult(asset);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            LastSaveChangesCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }

        public void Seed(Asset asset)
        {
            ArgumentNullException.ThrowIfNull(asset);

            _assets.Add(asset);
        }
    }
}