namespace AssetManagement.Application.Stores
{
    public sealed class StoreNotFoundException: Exception
    {
        public StoreNotFoundException(Guid id) : base($"The store with identifier '{id}' was not found.")
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}

