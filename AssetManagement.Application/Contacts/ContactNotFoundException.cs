namespace AssetManagement.Application.Contacts
{
    public sealed class ContactNotFoundException : Exception
    {
        public ContactNotFoundException(Guid id) : base($"The contact with identifier '{id}' was not found.")
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}
