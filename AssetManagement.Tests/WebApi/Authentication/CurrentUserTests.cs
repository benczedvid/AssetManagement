using System.Security.Claims;
using AssetManagement.WebApi.Authentication;
using Microsoft.AspNetCore.Http;

namespace AssetManagement.Tests.WebApi.Authentication;

[TestClass]
public class CurrentUserTests
{
    [TestMethod]
    public void Properties_Should_Return_Values_From_Claims()
    {
        // Arrange
        var entraObjectId = Guid.NewGuid();
        var entraTenantId = Guid.NewGuid();

        const string firstName = "Luke";
        const string lastName = "Skywalker";
        const string displayName = "Luke Skywalker";

        var currentUser = CreateCurrentUser(
            isAuthenticated: true,
            new Claim("oid", entraObjectId.ToString()),
            new Claim("tid", entraTenantId.ToString()),
            new Claim("given_name", firstName),
            new Claim("family_name", lastName),
            new Claim("name", displayName));

        // Act & Assert
        Assert.IsTrue(currentUser.IsAuthenticated);

        Assert.AreEqual(
            entraObjectId,
            currentUser.EntraObjectId);

        Assert.AreEqual(
            entraTenantId,
            currentUser.EntraTenantId);

        Assert.AreEqual(
            firstName,
            currentUser.FirstName);

        Assert.AreEqual(
            lastName,
            currentUser.LastName);

        Assert.AreEqual(
            displayName,
            currentUser.DisplayName);
    }

    [TestMethod]
    public void IsAuthenticated_Should_Return_False_When_Identity_Is_Not_Authenticated()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            isAuthenticated: false);

        // Act
        var result = currentUser.IsAuthenticated;

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EntraObjectId_Should_Throw_When_Oid_Claim_Is_Missing()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            isAuthenticated: true,
            new Claim("tid", Guid.NewGuid().ToString()));

        // Act & Assert
        Assert.ThrowsExactly<CurrentUserClaimException>(
            () => _ = currentUser.EntraObjectId);
    }

    [TestMethod]
    public void EntraObjectId_Should_Throw_When_Oid_Claim_Is_Not_A_Valid_Guid()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            isAuthenticated: true,
            new Claim("oid", "not-a-guid"),
            new Claim("tid", Guid.NewGuid().ToString()));

        // Act & Assert
        Assert.ThrowsExactly<CurrentUserClaimException>(
            () => _ = currentUser.EntraObjectId);
    }

    [TestMethod]
    public void EntraTenantId_Should_Throw_When_Tid_Claim_Is_Missing()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            isAuthenticated: true,
            new Claim("oid", Guid.NewGuid().ToString()));

        // Act & Assert
        Assert.ThrowsExactly<CurrentUserClaimException>(
            () => _ = currentUser.EntraTenantId);
    }

    [TestMethod]
    public void EntraTenantId_Should_Throw_When_Tid_Claim_Is_Not_A_Valid_Guid()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            isAuthenticated: true,
            new Claim("oid", Guid.NewGuid().ToString()),
            new Claim("tid", "not-a-guid"));

        // Act & Assert
        Assert.ThrowsExactly<CurrentUserClaimException>(
            () => _ = currentUser.EntraTenantId);
    }

    [TestMethod]
    public void FirstName_Should_Throw_When_GivenName_Claim_Is_Missing()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            isAuthenticated: true);

        // Act & Assert
        Assert.ThrowsExactly<CurrentUserClaimException>(
            () => _ = currentUser.FirstName);
    }

    [TestMethod]
    public void LastName_Should_Throw_When_FamilyName_Claim_Is_Missing()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            isAuthenticated: true);

        // Act & Assert
        Assert.ThrowsExactly<CurrentUserClaimException>(
            () => _ = currentUser.LastName);
    }

    [TestMethod]
    public void DisplayName_Should_Throw_When_Name_Claim_Is_Missing()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            isAuthenticated: true);

        // Act & Assert
        Assert.ThrowsExactly<CurrentUserClaimException>(
            () => _ = currentUser.DisplayName);
    }

    [TestMethod]
    public void Constructor_Should_Throw_When_HttpContextAccessor_Is_Null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new CurrentUser(null!));
    }

    private static CurrentUser CreateCurrentUser(
        bool isAuthenticated,
        params Claim[] claims)
    {
        var authenticationType = isAuthenticated
            ? "TestAuthentication"
            : null;

        var identity = new ClaimsIdentity(
            claims,
            authenticationType);

        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = httpContext
            };

        return new CurrentUser(httpContextAccessor);
    }
}