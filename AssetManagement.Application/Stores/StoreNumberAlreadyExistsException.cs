namespace AssetManagement.Application.Stores
{
    public sealed class StoreNumberAlreadyExistsException : Exception
    {
        public StoreNumberAlreadyExistsException(string storeNumber): base($"A store with store number {storeNumber} already exists."){
            StoreNumber = storeNumber;
        }

        public string StoreNumber { get; }
    }
}
