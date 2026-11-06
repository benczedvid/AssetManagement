namespace AssetManagement.Application.Users
{
    public sealed class ApplicationUserNotFoundException : Exception
    {
        public ApplicationUserNotFoundException(Guid id) : base($"The user with identifier '{id}' was not found.")
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}

