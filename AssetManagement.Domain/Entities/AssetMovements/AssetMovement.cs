namespace AssetManagement.Domain.Entities.AssetMovements
{
    public sealed class AssetMovement
    {
        public Guid Id { get; private set; }
        public Guid AssetId {  get; private set; }
        public Guid StoreId { get; private set; }
        public string EmployeeNumber { get; private set; } = null!;
        public AssetMovementType Type { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        private AssetMovement() { }

        private AssetMovement(Guid assetId, Guid storeId, string employeeNumber, AssetMovementType type)
        {
            Id = Guid.NewGuid();
            AssetId = assetId;
            StoreId = storeId;
            EmployeeNumber = employeeNumber.Trim();
            Type = type;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public static AssetMovement Create(Guid assetId,Guid storeId, string employeeNumber, AssetMovementType type)
        {
            if (assetId == Guid.Empty)
            {
                throw new ArgumentException("Az eszköz azonosítója nem lehet üres.", nameof(assetId));
            }
            if (storeId == Guid.Empty)
            {
                throw new ArgumentException("Az áruház azonosítója nem lehet üres.", nameof(storeId));
            }
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                throw new ArgumentException("A dolgozó törzsszáma kötelező.", nameof(employeeNumber));
            }
            if (!Enum.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Ismeretlen eszközmozgás-típus.");
            }

            return new AssetMovement(
                assetId: assetId,
                storeId: storeId,
                employeeNumber:  employeeNumber,
                type:  type);
        }
    }
}
