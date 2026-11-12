using AssetManagement.Application.AssetMovements.Exceptions;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Assets.DisposeAsset;
using AssetManagement.Application.Employees;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Application.ServiceDesk.Exceptions;
using AssetManagement.Application.Stores;
using AssetManagement.Application.Users;
using AssetManagement.Application.Vendors;
using AssetManagement.Domain.Entities.Assets.Exceptions;
using AssetManagement.WebApi.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AssetManagement.WebApi.ExceptionHandling;

/// <summary>
/// Converts unhandled application exceptions into RFC-compatible
/// problem details responses.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(problemDetailsService);

        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);

        var problemDetails = CreateProblemDetails(context, exception);

        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        LogException(context, exception, statusCode);

        context.Response.StatusCode = statusCode;

        var problemDetailsContext = new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails,
                Exception = exception
            };

        return await _problemDetailsService.TryWriteAsync(problemDetailsContext);
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            CurrentUserClaimException => CreateUnauthorizedProblemDetails(context),
            PackageNotFoundException => CreatePackageProblemDetails(
                    context,
                    StatusCodes.Status404NotFound,
                    "The requested package was not found.",
                    "The requested package does not exist."),

            PackageStoreNotFoundException => CreatePackageProblemDetails(
                    context,
                    StatusCodes.Status404NotFound,
                    "A store required by the package operation " +
                    "was not found.",
                    "The source or destination store does not exist."),

            PackageAssetNotFoundException => CreatePackageProblemDetails(
                    context,
                    StatusCodes.Status404NotFound,
                    "An asset required by the package operation was not found.",
                    "One of the package assets does not exist."),

            PackageCourierNotFoundException =>  CreatePackageProblemDetails(
                    context,
                    StatusCodes.Status404NotFound,
                    "The selected package courier was not found.",
                    "The selected courier does not exist."),

            PackageAccessDeniedException => CreatePackageProblemDetails(
                    context,
                    StatusCodes.Status403Forbidden,
                    "Access to the package operation is denied.",
                    "The current user is not authorized to access this package or perform the requested operation."),

            PackageStateConflictException => CreatePackageProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    "The package operation conflicts with the current state.",
                    exception.Message),

            UnauthorizedAccessException => CreateUnauthorizedProblemDetails(context),
            StoreNumberAlreadyExistsException => CreateConflictProblemDetails(context),
            ArgumentException => CreateBadRequestProblemDetails(context),
            StoreNotFoundException => StoreNotFoundProblemDetails(context),
            SerialNumberAlreadyExistsException => CreateAssetConflictProblemDetails(context),
            AssetNotFoundException => AssetNotFoundProblemDetails(context),
            AssetDisposalStateConflictException => CreateAssetDisposalProblemDetails(
                context,
                StatusCodes.Status409Conflict,
                "The asset disposal operation conflicts with the current state.",
                exception.Message),
            AssetDisposalAccessDeniedException => CreateAssetDisposalProblemDetails(
                context,
                StatusCodes.Status403Forbidden,
                "Access to the asset disposal operation is denied.",
                exception.Message),

            AssetStateConflictException => CreateAssetStateConflictProblemDetails(context, exception.Message),

            TimeoutException => CreateAssetLockTimeoutProblemDetails(context),
            VendorNotFoundException => VendorNotFoundProblemDetails(context),
            VendorAlreadyExistsException => VendorAlreadyExistsProblemDetails(context),
            EmployeeNotFoundException => EmployeeNotFoundProblemDetails(context),
            ApplicationUserNotFoundException => ApplicationUserNotFoundProblemDetails(context),
            ServiceCaseNotFoundException => CreateServiceCaseProblemDetails(
                    context,
                    StatusCodes.Status404NotFound,
                    "The requested service case was not found.",
                    "The requested service case does not exist."),

            ServiceCaseAlreadyExistsException => CreateServiceCaseProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    "An active service case already exists.",
                    "The asset already has an active service case."),

            ServiceCaseStateConflictException => CreateServiceCaseProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    "The service case operation conflicts with the current state.",
                    exception.Message),

            ServiceCaseAccessDeniedException => CreateServiceCaseProblemDetails(
                    context,
                    StatusCodes.Status403Forbidden,
                    "Access to the service case operation is denied.",
                    "The current user is not authorized to access this service case or perform the requested " +
                    "operation."),

            ServiceDeskReconciliationConflictException => CreateServiceDeskProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    "The ServiceDesk reconciliation conflicts with the current state.",
                    "The outbox state or processing operation identifier has changed."),

            AssetMovementStateConflictException => CreateAssetMovementConflictProblemDetails(context, exception.Message),

            _ =>
                CreateInternalServerErrorProblemDetails(context)
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        return problemDetails;
    }

    private static ProblemDetails CreatePackageProblemDetails(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(statusCode),
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateUnauthorizedProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status401Unauthorized),
            Title = "Authentication information is invalid.",
            Status = StatusCodes.Status401Unauthorized,
            Detail ="The authenticated identity does not contain all required user information.",
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateConflictProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status409Conflict),
            Title = "A resource conflict occurred.",
            Status = StatusCodes.Status409Conflict,
            Detail =  "A store with the specified store number already exists.",
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateBadRequestProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status400BadRequest),
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
            Type = ResolveProblemType(StatusCodes.Status500InternalServerError),
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The server encountered an unexpected error while processing the request.",
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails StoreNotFoundProblemDetails(HttpContext context)
    {
        return CreateNotFoundProblemDetails(context, "The requested store does not exist.");
    }

    private static ProblemDetails CreateAssetConflictProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status409Conflict),
            Title = "A resource conflict occurred.",
            Status = StatusCodes.Status409Conflict,
            Detail = "The asset with the specified serial number already exists.",
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails AssetNotFoundProblemDetails(HttpContext context)
    {
        return CreateNotFoundProblemDetails(context, "The requested asset does not exist.");
    }

    private static ProblemDetails VendorNotFoundProblemDetails(HttpContext context)
    {
        return CreateNotFoundProblemDetails(context, "The requested vendor does not exist.");
    }

    private static ProblemDetails VendorAlreadyExistsProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status409Conflict),
            Title = "A resource conflict occurred.",
            Status = StatusCodes.Status409Conflict,
            Detail = "The vendor with the specified identifier already exists.",
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails EmployeeNotFoundProblemDetails(HttpContext context)
    {
        return CreateNotFoundProblemDetails(context, "The requested employee does not exist.");
    }

    private static ProblemDetails ApplicationUserNotFoundProblemDetails(HttpContext context)
    {
        return CreateNotFoundProblemDetails(context, "The requested application user does not exist.");
    }

    private static ProblemDetails CreateNotFoundProblemDetails(HttpContext context, string detail)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status404NotFound),
            Title = "The requested resource was not found.",
            Status = StatusCodes.Status404NotFound,
            Detail = detail,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateServiceCaseProblemDetails(HttpContext context,
            int statusCode,
            string title,
            string detail)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(statusCode),
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateAssetDisposalProblemDetails(HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(statusCode),
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateAssetLockTimeoutProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status503ServiceUnavailable),
            Title = "The asset operation is temporarily unavailable.",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail ="The asset is currently being modified by another operation. Retry the request after a short delay.",
            Instance = context.Request.Path
        };
    }

    private static string ResolveProblemType(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            StatusCodes.Status401Unauthorized =>"https://tools.ietf.org/html/rfc9110#section-15.5.2",
            StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            StatusCodes.Status503ServiceUnavailable => "https://tools.ietf.org/html/rfc9110#section-15.6.4",

            _ =>
                "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };
    }

    private void LogException(HttpContext context, Exception exception, int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred while processing {RequestMethod} {RequestPath}. Trace identifier: {TraceId}.",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            return;
        }

        _logger.LogWarning(
            exception,
            "A handled exception occurred while processing {RequestMethod} {RequestPath}. HTTP status: {StatusCode}. " +
            "Trace identifier: {TraceId}.",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            context.TraceIdentifier);
    }
    private static ProblemDetails CreateServiceDeskProblemDetails(HttpContext context, int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(statusCode),
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
    }
    private static ProblemDetails CreateAssetMovementConflictProblemDetails(HttpContext context, string detail)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status409Conflict),
            Title = "The asset movement conflicts with the current asset state.",
            Status = StatusCodes.Status409Conflict,
            Detail = detail,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateAssetStateConflictProblemDetails(HttpContext context, string detail)
    {
        return new ProblemDetails
        {
            Type = ResolveProblemType(StatusCodes.Status409Conflict),
            Title = "The asset operation conflicts with the current asset state.",
            Status = StatusCodes.Status409Conflict,
            Detail = detail,
            Instance = context.Request.Path
        };
    }
}