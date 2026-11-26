namespace AssetManagement.Domain.Entities.Vendors.Exceptions;

/// <summary>
/// Thrown when attaching or detaching a contact conflicts with the current
/// state of the vendor or the contact.
/// </summary>
public sealed class VendorContactConflictException : InvalidOperationException
{
    public VendorContactConflictException(string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }
}
