using System.Text.Json;
using AssetManagement.WebApi.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssetManagement.Tests.WebApi.ExceptionHandling;

[TestClass]
public sealed class GlobalExceptionHandlerTests
{
    [TestMethod]
    public async Task TryHandleAsync_Should_Return_InternalServerError_For_Unexpected_Exception()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddOptions();
        services.AddLogging();
        services.AddProblemDetails();

        await using var serviceProvider =
            services.BuildServiceProvider();

        var problemDetailsService =
            serviceProvider.GetRequiredService<
                IProblemDetailsService>();

        var handler =
            new GlobalExceptionHandler(
                NullLogger<GlobalExceptionHandler>.Instance,
                problemDetailsService);

        var httpContext =
            new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

        httpContext.Request.Path =
            "/api/test";

        httpContext.Response.Body =
            new MemoryStream();

        var exception =
            new Exception(
                "Sensitive internal information.");

        // Act
        var handled =
            await handler.TryHandleAsync(
                httpContext,
                exception,
                CancellationToken.None);

        // Assert
        Assert.IsTrue(handled);

        Assert.AreEqual(
            StatusCodes.Status500InternalServerError,
            httpContext.Response.StatusCode);

        Assert.AreEqual(
            "application/problem+json",
            httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;

        using var jsonDocument =
            await JsonDocument.ParseAsync(
                httpContext.Response.Body);

        var root =
            jsonDocument.RootElement;

        Assert.AreEqual(
            "An unexpected error occurred.",
            root.GetProperty("title")
                .GetString());

        Assert.AreEqual(
            StatusCodes.Status500InternalServerError,
            root.GetProperty("status")
                .GetInt32());

        Assert.AreEqual(
            "/api/test",
            root.GetProperty("instance")
                .GetString());

        Assert.IsTrue(
            root.TryGetProperty(
                "traceId",
                out _));

        Assert.IsFalse(
            root.GetRawText().Contains(
                "Sensitive internal information.",
                StringComparison.Ordinal));
    }
}