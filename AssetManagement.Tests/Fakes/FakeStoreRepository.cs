using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Domain.Entities.Stores;

namespace AssetManagement.Tests.Fakes;

internal sealed class FakeStoreRepository : IStoreRepository
{
    private readonly List<Store> _stores = [];

    public IReadOnlyCollection<Store> Stores =>
        _stores.AsReadOnly();

    public int AddCallCount { get; private set; }
    public int SaveChangesCallCount { get; private set; }
    public int GetAllCallCount { get; private set; }
    public int GetByIdCallCount { get; private set; }
    public int GetByIdsCallCount { get; private set; }
    public int GetByStoreNumberCallCount { get; private set; }
    public int GetForUpdateByIdCallCount { get; private set; }

    public CancellationToken LastAddCancellationToken { get; private set; }
    public CancellationToken LastSaveChangesCancellationToken { get; private set; }
    public CancellationToken LastGetAllCancellationToken { get; private set; }
    public CancellationToken LastGetByIdCancellationToken { get; private set; }
    public CancellationToken LastGetByIdsCancellationToken {  get; private set; }
    public CancellationToken LastGetByStoreNumberCancellationToken { get; private set; }
    public CancellationToken LastGetForUpdateByIdCancellationToken { get; private set; }

    public Task AddAsync(
        Store store,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);

        AddCallCount++;
        LastAddCancellationToken = cancellationToken;

        _stores.Add(store);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Store>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        GetAllCallCount++;
        LastGetAllCancellationToken = cancellationToken;

        IReadOnlyList<Store> stores = _stores.ToList();

        return Task.FromResult(stores);
    }

    public Task<Store?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        GetByIdCallCount++;
        LastGetByIdCancellationToken = cancellationToken;

        var store = _stores.SingleOrDefault(store => store.Id == id);
        return Task.FromResult(store);
    }

    public Task<IReadOnlyList<Store>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        GetByIdsCallCount++;
        LastGetByIdsCancellationToken = cancellationToken;
        var requestedIds = ids.ToHashSet();
        IReadOnlyList<Store> stores = _stores.Where(store => requestedIds.Contains(store.Id)).ToList();
        return Task.FromResult(stores);
    }

    public Task<Store?> GetByStoreNumberAsync(
        string storeNumber,
        CancellationToken cancellationToken = default)
    {
        GetByStoreNumberCallCount++;
        LastGetByStoreNumberCancellationToken = cancellationToken;

        var store = _stores.SingleOrDefault(
            store => store.StoreNumber == storeNumber);

        return Task.FromResult(store);
    }

    public Task<Store?> GetForUpdateByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        GetForUpdateByIdCallCount++;
        LastGetForUpdateByIdCancellationToken = cancellationToken;

        var store = _stores.SingleOrDefault(store => store.Id == id);

        return Task.FromResult(store);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        LastSaveChangesCancellationToken = cancellationToken;

        return Task.CompletedTask;
    }

    public void Seed(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _stores.Add(store);
    }
}