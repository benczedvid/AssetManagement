namespace AssetManagement.WebApi.Authentication
{
    public sealed class CurrentUserClaimException: Exception
    {
        public CurrentUserClaimException(string message) :  base(message) { }
    }
}
