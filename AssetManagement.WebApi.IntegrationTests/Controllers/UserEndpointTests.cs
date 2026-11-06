using AssetManagement.Application.Users.GetOrCreateCurrentUser;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AssetManagement.WebApi.IntegrationTests.Controllers;

[TestClass]
[DoNotParallelize]
public sealed class UsersEndpointTests
{
    private AssetManagementWebApplicationFactory? _factory = null!;

    private HttpClient? _client = null!;

    [TestInitialize]
    public void Initialize()
    {
        _factory = new AssetManagementWebApplicationFactory();

        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task GetMe_Should_Return_Unauthorized_When_Request_Is_Not_Authenticated()
    {
        // Act
        var response = await _client!.GetAsync("/api/users/me");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetMe_Should_Create_And_Return_Current_User()
    {
        // Arrange
        var entraObjectId = Guid.NewGuid();
        var entraTenantId = Guid.NewGuid();

        AddAuthenticatedUserHeaders(
            entraObjectId,
            entraTenantId);

        // Act
        var response =
            await _client!.GetAsync(
                "/api/users/me");

        // Assert
        Assert.AreEqual(
            HttpStatusCode.OK,
            response.StatusCode);

        var currentUser =
            await response.Content
                .ReadFromJsonAsync<CurrentUserResponse>();

        Assert.IsNotNull(currentUser);

        Assert.AreEqual(
            entraObjectId,
            currentUser.EntraObjectId);

        Assert.AreEqual(
            entraTenantId,
            currentUser.EntraTenantId);

        Assert.AreEqual(
            "Luke",
            currentUser.FirstName);

        Assert.AreEqual(
            "Skywalker",
            currentUser.LastName);

        Assert.AreEqual(
            "Luke Skywalker",
            currentUser.DisplayName);

        Assert.IsTrue(currentUser.IsActive);

        await AssertUserWasPersistedAsync(
            currentUser.Id,
            entraObjectId,
            entraTenantId);
    }

    [TestMethod]
    public async Task GetMe_Should_Return_Same_User_When_Called_Twice()
    {
        // Arrange
        var entraObjectId = Guid.NewGuid();
        var entraTenantId = Guid.NewGuid();

        AddAuthenticatedUserHeaders(
            entraObjectId,
            entraTenantId);

        // Act
        var firstResponse =
            await _client!.GetFromJsonAsync<CurrentUserResponse>(
                "/api/users/me");

        var secondResponse =
            await _client!.GetFromJsonAsync<CurrentUserResponse>(
                "/api/users/me");

        // Assert
        Assert.IsNotNull(firstResponse);
        Assert.IsNotNull(secondResponse);

        Assert.AreEqual(
            firstResponse.Id,
            secondResponse.Id);

        await AssertOnlyOneMatchingUserExistsAsync(
            entraObjectId,
            entraTenantId);
    }
    [TestMethod]
    public async Task GetMe_Should_Return_Unauthorized_ProblemDetails_When_Required_Claim_Is_Missing()
    {
        // Arrange
        var entraObjectId = Guid.NewGuid();
        var entraTenantId = Guid.NewGuid();

        AddAuthenticatedUserHeaders(
            entraObjectId,
            entraTenantId);

        _client!.DefaultRequestHeaders.Add(
            TestAuthenticationHandler
                .OmitLastNameHeader,
            "true");

        // Act
        var response =
            await _client!.GetAsync(
                "/api/users/me");

        // Assert
        Assert.AreEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.IsNotNull(problemDetails);

        Assert.AreEqual(StatusCodes.Status401Unauthorized, problemDetails.Status);

        Assert.AreEqual("Authentication information is invalid.", problemDetails.Title);

        Assert.AreEqual("/api/users/me", problemDetails.Instance);

        Assert.IsTrue(problemDetails.Extensions.ContainsKey("traceId"));
    }

    private void AddAuthenticatedUserHeaders(
        Guid entraObjectId,
        Guid entraTenantId)
    {
        _client!.DefaultRequestHeaders.Add(
            TestAuthenticationHandler
                .AuthenticationHeader,
            "true");

        _client!.DefaultRequestHeaders.Add(
            TestAuthenticationHandler
                .EntraObjectIdHeader,
            entraObjectId.ToString());

        _client!.DefaultRequestHeaders.Add(
            TestAuthenticationHandler
                .EntraTenantIdHeader,
            entraTenantId.ToString());
    }

    private async Task AssertUserWasPersistedAsync(
        Guid userId,
        Guid entraObjectId,
        Guid entraTenantId)
    {
        using var scope = _factory!.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedUser = await dbContext.ApplicationUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Id == userId);

        Assert.IsNotNull(persistedUser);

        Assert.AreEqual(entraObjectId, persistedUser.EntraObjectId);

        Assert.AreEqual(entraTenantId, persistedUser.EntraTenantId);
    }

    private async Task
        AssertOnlyOneMatchingUserExistsAsync(
            Guid entraObjectId,
            Guid entraTenantId)
    {
        using var scope =
            _factory!.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var matchingUsersCount =
            await dbContext.ApplicationUsers
                .AsNoTracking()
                .CountAsync(
                    user =>
                        user.EntraObjectId ==
                        entraObjectId &&
                        user.EntraTenantId ==
                        entraTenantId);

        Assert.AreEqual(1, matchingUsersCount);
    }
}