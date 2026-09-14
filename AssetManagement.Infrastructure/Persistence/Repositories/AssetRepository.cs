using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

public sealed class AssetRepository : IAssetRepository
{
    private readonly AppDbContext _dbContext;

    public AssetRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);

        await _dbContext.Assets.AddAsync(asset, cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Assets.AsNoTracking().Include(asset => asset.AssignedVendor).ToListAsync(cancellationToken);
    }

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Assets.AsNoTracking().Include(asset => asset.AssignedVendor).SingleOrDefaultAsync(asset => asset.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetByAssignedUserIdAsync(Guid assignedUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Assets.AsNoTracking().Where(asset => asset.AssignedUserId == assignedUserId).ToListAsync(cancellationToken);
    }

    public Task<Asset?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        return _dbContext.Assets.AsNoTracking().SingleOrDefaultAsync(asset => asset.SerialNumber == serialNumber, cancellationToken);
    }
    public Task<Asset?> GetByRfidTagIdAsync(string rfidTagId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rfidTagId);
        return _dbContext.Assets.SingleOrDefaultAsync(asset => asset.RfidTagId == rfidTagId, cancellationToken);
    }
    public Task<Asset?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Assets.SingleOrDefaultAsync(asset => asset.Id == id, cancellationToken);
    }
    public Task<Asset?> GetForUpdateBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.Assets.SingleOrDefaultAsync(asset => asset.SerialNumber == serialNumber, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        if(storeId == Guid.Empty)
        {
            throw new ArgumentException("The store identifier cannot be empty.", nameof(storeId));

        }
        return await _dbContext.Assets
                .AsNoTracking()
                .Include(asset => asset.AssignedVendor)
                .Where(asset => asset.AssignedStoreId == storeId)
                .ToListAsync(cancellationToken);
    }

    public Task<Asset?> GetByIdAndStoreIdAsync(Guid id, Guid storeId, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The asset identifier cannot be empty.", nameof(id));
        }
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("The store identifier cannot be empty.", nameof(storeId));
        }
        return _dbContext.Assets
            .AsNoTracking()
            .Include(asset => asset.AssignedVendor)
            .SingleOrDefaultAsync(
                asset => asset.Id == id && asset.AssignedStoreId == storeId,
                cancellationToken);
    }
}