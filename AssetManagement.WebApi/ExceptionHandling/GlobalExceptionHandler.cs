using System.Diagnostics;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Stores;
using AssetManagement.WebApi.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.WebApi.ExceptionHandling;

/// <summary>
/// Handles unhandled exceptions raised while processing HTTP requests
/// and converts them into standardized Problem Details responses.
/// </summary>
public sealed class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record request exceptions.
    /// </param>
    /// <param name="problemDetailsService">
    /// The service used to write Problem Details responses.
    /// </param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger,IProblemDetailsService problemDetailsService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(problemDetailsService);

        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);

        var problemDetails = CreateProblemDetails(context, exception);

        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        LogException(context, exception, statusCode);

        context.Response.StatusCode = statusCode;

        var problemDetailsContext =
            new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails,
                Exception = exception
            };

        return await _problemDetailsService.TryWriteAsync(problemDetailsContext);
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        Exception exception)
    {
        var problemDetails =
            exception switch
            {
                CurrentUserClaimException =>
                    CreateUnauthorizedProblemDetails(context),
                UnauthorizedAccessException =>
                    CreateUnauthorizedProblemDetails(context),
                StoreNumberAlreadyExistsException =>
                    CreateConflictProblemDetails(context),
                ArgumentException =>
                    CreateBadRequestProblemDetails(context),
                StoreNotFoundException =>
                    StoreNotFoundProblemDetails(context),
                SerialNumberAlreadyExistsException =>
                    CreateAssetConflictProblemDetails(context),
                AssetNotFoundException =>
                    AssetNotFoundProblemDetails(context),

                _ =>
                    CreateInternalServerErrorProblemDetails(
                        context)
            };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id
            ?? context.TraceIdentifier;

        return problemDetails;
    }

    private static ProblemDetails CreateUnauthorizedProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            Title = "Authentication information is invalid.",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "The authenticated identity does not contain all required user information.",
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateConflictProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            Title = "A resource conflict occurred.",
            Status = StatusCodes.Status409Conflict,
            Detail = "A store with the specified store number already exists.",
            Instance = context.Request.Path
        };
    }
    private static ProblemDetails CreateBadRequestProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "The request is invalid.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more request values are invalid.",
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateInternalServerErrorProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The server encountered an unexpected error while processing the request.",
            Instance = context.Request.Path
        };
    }
    private static ProblemDetails StoreNotFoundProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Title = "The requested resource was not found.",
            Status = StatusCodes.Status404NotFound,
            Detail = "The requested store does not exist.",
            Instance = context.Request.Path
        };
    }
    private static ProblemDetails CreateAssetConflictProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            Title = "A resource conflict occurred.",
            Status = StatusCodes.Status409Conflict,
            Detail = "The asset  with the specified serial number already exists.",
            Instance = context.Request.Path
        };
    }
    private static ProblemDetails AssetNotFoundProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Title = "The requested resource was not found.",
            Status = StatusCodes.Status404NotFound,
            Detail = "The requested asset does not exist.",
            Instance = context.Request.Path
        };
    }

    private void LogException(
        HttpContext context,
        Exception exception,
        int statusCode)
    {
        if (statusCode >=
            StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred while processing " +
                "{RequestMethod} {RequestPath}. " +
                "Trace identifier: {TraceId}.",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            return;
        }

        _logger.LogWarning(
            exception,
            "A request exception occurred while processing " +
            "{RequestMethod} {RequestPath}. " +
            "Trace identifier: {TraceId}.",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);
    }
}