using System.Globalization;
using System.Net;
using System.Text;
using AssetManagement.Application.Common.Email;
using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Application.Common.Interfaces.Email;
using AssetManagement.Application.Common.Interfaces.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssetManagement.Application.AssetMovements.AssetMovementReports;

public sealed class EndOfDayAssetReportService : IEndOfDayAssetReportService
{
    private readonly IUnreturnedEmployeeAssetReportRepository _reportRepository;
    private readonly IEmailSender _emailSender;
    private readonly EndOfDayAssetReportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EndOfDayAssetReportService> _logger;
    private readonly IStoreRepository _storeRepository;

    public EndOfDayAssetReportService(
        IUnreturnedEmployeeAssetReportRepository reportRepository,
        IEmailSender emailSender,
        IOptions<EndOfDayAssetReportOptions> options,
        TimeProvider timeProvider,
        ILogger<EndOfDayAssetReportService> logger,
        IStoreRepository storeRepository)
    {
        ArgumentNullException.ThrowIfNull(reportRepository);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(storeRepository);

        _reportRepository = reportRepository;
        _emailSender = emailSender;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
        _storeRepository = storeRepository;
    }

    public async Task<DailyAssetReportResult> SendAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOptions();

        var generatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var reportRows = await _reportRepository.GetUnreturnedEmployeeAssetsAsync(cancellationToken);
        var stores = await _storeRepository.GetAllAsync(cancellationToken);
        var groupsWithRows = reportRows
            .GroupBy(row => new
            {
                row.StoreId,
                row.StoreNumber,
                row.StoreName
            })
            .Select(group => new
            {
                Key = group.Key,
                Rows = group
                    .OrderBy(row => row.EmployeeFullName)
                    .ThenBy(row => row.CheckedOutAtUtc)
                    .ToArray()
            })
            .ToArray();

        var storeIdsWithRows = reportRows
            .Select(row => row.StoreId)
            .ToHashSet();

        var emptyStoreGroups = stores
            .Where(store => !storeIdsWithRows.Contains(store.Id))
            .Select(store => new
            {
                Key = new
                {
                    StoreId = store.Id,
                    StoreNumber = store.StoreNumber,
                    StoreName = store.Name
                },
                Rows = Array.Empty<UnreturnedEmployeeAssetReportRow>()
            });

        var storeGroups = groupsWithRows
            .Concat(emptyStoreGroups)
            .OrderBy(group => group.Key.StoreNumber)
            .ThenBy(group => group.Key.StoreId)
            .ToArray();

        var sentEmailCount = 0;
        var skippedStoreCount = 0;
        var storeFailures = new List<Exception>();

        foreach (var storeGroup in storeGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var rows = storeGroup.Rows;

                if (rows.Length == 0 && !_options.SendEmptyReports)
                {
                    skippedStoreCount++;
                    continue;
                }

                var recipientAddresses = ResolveRecipientAddresses(storeGroup.Key.StoreNumber);

                var message = CreateEmailMessage(
                    storeNumber: storeGroup.Key.StoreNumber,
                    storeName: storeGroup.Key.StoreName,
                    recipientAddresses: recipientAddresses,
                    rows: rows,
                    generatedAtUtc: generatedAtUtc);

                await _emailSender.SendAsync(message, cancellationToken);

                sentEmailCount++;

                _logger.LogInformation(
                    "The asset report send operation completed. " +
                    "StoreId: {StoreId}, " +
                    "StoreNumber: {StoreNumber}, " +
                    "RecipientCount: {RecipientCount}, " +
                    "UnreturnedAssetCount: {UnreturnedAssetCount}",
                    storeGroup.Key.StoreId,
                    storeGroup.Key.StoreNumber,
                    recipientAddresses.Count,
                    rows.Length);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "The asset report run was cancelled. " +
                    "StoreId: {StoreId}, " +
                    "StoreNumber: {StoreNumber}, " +
                    "SentEmailCount: {SentEmailCount}, " +
                    "FailedStoreCount: {FailedStoreCount}",
                    storeGroup.Key.StoreId,
                    storeGroup.Key.StoreNumber,
                    sentEmailCount,
                    storeFailures.Count);

                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The asset report could not be prepared or sent. " +
                    "Processing will continue with the next store. " +
                    "StoreId: {StoreId}, " +
                    "StoreNumber: {StoreNumber}",
                    storeGroup.Key.StoreId,
                    storeGroup.Key.StoreNumber);

                storeFailures.Add( new InvalidOperationException(
                        $"The asset report failed for store " +
                        $"'{storeGroup.Key.StoreNumber}' " +
                        $"({storeGroup.Key.StoreId}).",
                        exception));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = new DailyAssetReportResult(
            StoreCount: storeGroups.Length,
            SentEmailCount: sentEmailCount,
            SkippedStoreCount: skippedStoreCount,
            UnreturnedAssetCount: reportRows.Count,
            GeneratedAtUtc: generatedAtUtc);

        if (storeFailures.Count > 0)
        {
            _logger.LogWarning(
                "The asset report run finished with store failures. " +
                "StoreCount: {StoreCount}, " +
                "SentEmailCount: {SentEmailCount}, " +
                "SkippedStoreCount: {SkippedStoreCount}, " +
                "FailedStoreCount: {FailedStoreCount}, " +
                "UnreturnedAssetCount: {UnreturnedAssetCount}, " +
                "GeneratedAtUtc: {GeneratedAtUtc}",
                result.StoreCount,
                result.SentEmailCount,
                result.SkippedStoreCount,
                storeFailures.Count,
                result.UnreturnedAssetCount,
                result.GeneratedAtUtc);

            throw new AggregateException(
                $"The asset report run finished with " +
                $"{storeFailures.Count} store failure(s). " +
                $"Completed email send operations: {sentEmailCount}.",
                storeFailures);
        }

        _logger.LogInformation(
            "The asset report run completed successfully. " +
            "StoreCount: {StoreCount}, " +
            "SentEmailCount: {SentEmailCount}, " +
            "SkippedStoreCount: {SkippedStoreCount}, " +
            "UnreturnedAssetCount: {UnreturnedAssetCount}, " +
            "GeneratedAtUtc: {GeneratedAtUtc}",
            result.StoreCount,
            result.SentEmailCount,
            result.SkippedStoreCount,
            result.UnreturnedAssetCount,
            result.GeneratedAtUtc);

        return result;
    }

    private IReadOnlyCollection<string>ResolveRecipientAddresses(string storeNumber)
    {
        if (_options.TestMode)
        {
            return NormalizeRecipientAddresses(_options.TestRecipientAddressesTemplate);
        }

        var normalizedStoreNumber = NormalizeStoreNumber(storeNumber);

        return _options.RecipientAddressesTemplate
            .Select(
                template => string.Format(
                    CultureInfo.InvariantCulture,
                    template.Trim(),
                    normalizedStoreNumber))
            .Select(address => address.Trim())
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyCollection<string>NormalizeRecipientAddresses(IEnumerable<string> addresses)
    {
        return addresses
            .Select(address => address.Trim())
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeStoreNumber(string storeNumber)
    {
        if (string.IsNullOrWhiteSpace(storeNumber))
        {
            throw new InvalidOperationException("The store number is required.");
        }

        var normalizedStoreNumber =storeNumber.Trim();

        if (normalizedStoreNumber.Length != 3 || !normalizedStoreNumber.All(char.IsDigit))
        {
            throw new InvalidOperationException("The store number must contain exactly three digits.");
        }

        return normalizedStoreNumber;
    }

    private EmailMessage CreateEmailMessage(
        string storeNumber,
        string storeName,
        IReadOnlyCollection<string> recipientAddresses,
        IReadOnlyCollection<UnreturnedEmployeeAssetReportRow> rows,
        DateTime generatedAtUtc)
    {
        var storeDisplayName =$"{storeNumber} - {storeName}";

        var reportSubject = string.Format(
            CultureInfo.InvariantCulture,
            _options.SubjectTemplate,
            storeDisplayName);

        var subject = _options.TestMode ? $"[TEST] {reportSubject}" : reportSubject;
        var generatedAtLocal =ConvertToLocalTime(generatedAtUtc);

        var textBody = CreateTextBody(storeDisplayName, rows, generatedAtLocal);
        var htmlBody = CreateHtmlBody(storeDisplayName, rows, generatedAtLocal);

        return new EmailMessage(
            ToRecipients:  recipientAddresses,
            Subject: subject,
            TextBody: textBody,
            HtmlBody: htmlBody);
    }

    private string CreateTextBody(
        string storeDisplayName,
        IReadOnlyCollection<UnreturnedEmployeeAssetReportRow> rows,
        DateTime generatedAtLocal)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Eszközkezelő Napi riport");
        builder.AppendLine();
        builder.AppendLine($"Áruház: {storeDisplayName}");
        builder.AppendLine($"Nem leadott eszközök száma: {rows.Count}");
        builder.AppendLine();

        foreach (var row in rows)
        {
            builder.AppendLine(
                $"{row.EmployeeFullName} | " +
                $"{row.EmployeeNumber} | " +
                $"{row.AssetName} | " +
                $"{row.AssetSerialNumber} | " +
                $"{FormatLocalTime(row.CheckedOutAtUtc)}");
        }

        builder.AppendLine();

        builder.AppendLine(
            "A listában szereplő eszközöket " +
            "felvették, de nem adták le a riport " +
            "készítésének időpontjáig.");

        builder.AppendLine();

        builder.AppendLine(
            $"Készült: {generatedAtLocal:yyyy-MM-dd HH:mm}");

        return builder.ToString();
    }

    private string CreateHtmlBody(
        string storeDisplayName,
        IReadOnlyCollection<UnreturnedEmployeeAssetReportRow> rows,
        DateTime generatedAtLocal)
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            """
            <!doctype html>
            <html lang="hu">
            <head>
                <meta charset="utf-8">
                <title>
                    AssetManagement napi riport
                </title>
            </head>

            <body style="
                margin: 0;
                padding: 24px;
                background-color: #f1f5f9;
                color: #0f172a;
                font-family: Arial, sans-serif;">
            """);

        builder.AppendLine(
            """
            <table
                role="presentation"
                width="100%"
                cellspacing="0"
                cellpadding="0"
                style="
                    max-width: 800px;
                    margin: 0 auto;
                    border-collapse: collapse;
                    background-color: #ffffff;
                    border: 1px solid #e2e8f0;">
            """);

        builder.AppendLine(
            """
            <tr>
                <td style="
                    padding: 20px 24px;
                    background-color: #002b51;
                    color: #ffffff;">

                    <strong style="font-size: 20px;">
                        AssetManagement
                    </strong>

                </td>
            </tr>
            """);

        builder.AppendLine(
            $"""
            <tr>
                <td style="padding: 24px;">

                    <h1 style="
                        margin: 0;
                        color: #002b51;
                        font-size: 22px;">
                        Napi riport
                    </h1>

                    <p style="
                        margin: 8px 0 0;
                        color: #475569;">
                        Áruház:
                        <strong>
                            {WebUtility.HtmlEncode(
                                storeDisplayName)}
                        </strong>
                    </p>

                    <p style="
                        margin: 8px 0 24px;
                        color: #475569;">
                        Nem leadott eszközök száma:
                        <strong>
                            {rows.Count}
                        </strong>
                    </p>
            """);

        builder.AppendLine(
            """
            <table
                width="100%"
                cellspacing="0"
                cellpadding="0"
                style="
                    width: 100%;
                    border-collapse: collapse;
                    border: 1px solid #cbd5e1;">

                <thead>
                    <tr style="
                        background-color: #eaf3f8;
                        color: #002b51;">

                        <th style="
                            padding: 10px;
                            border: 1px solid #cbd5e1;
                            text-align: left;">
                            Alkalmazott
                        </th>

                        <th style="
                            padding: 10px;
                            border: 1px solid #cbd5e1;
                            text-align: left;">
                            Törzsszám
                        </th>

                        <th style="
                            padding: 10px;
                            border: 1px solid #cbd5e1;
                            text-align: left;">
                            Eszköz
                        </th>

                        <th style="
                            padding: 10px;
                            border: 1px solid #cbd5e1;
                            text-align: left;">
                            Gyáriszám
                        </th>

                        <th style="
                            padding: 10px;
                            border: 1px solid #cbd5e1;
                            text-align: left;">
                            Felvétel ideje
                        </th>

                    </tr>
                </thead>

                <tbody>
            """);

        foreach (var row in rows)
        {
            builder.AppendLine(
                $"""
                <tr>
                    <td style="
                        padding: 10px;
                        border: 1px solid #cbd5e1;">
                        {WebUtility.HtmlEncode(
                            row.EmployeeFullName)}
                    </td>

                    <td style="
                        padding: 10px;
                        border: 1px solid #cbd5e1;">
                        {WebUtility.HtmlEncode(
                            row.EmployeeNumber)}
                    </td>

                    <td style="
                        padding: 10px;
                        border: 1px solid #cbd5e1;">
                        {WebUtility.HtmlEncode(
                            row.AssetName)}
                    </td>

                    <td style="
                        padding: 10px;
                        border: 1px solid #cbd5e1;">
                        {WebUtility.HtmlEncode(
                            row.AssetSerialNumber)}
                    </td>

                    <td style="
                        padding: 10px;
                        border: 1px solid #cbd5e1;">
                        {FormatLocalTime(
                            row.CheckedOutAtUtc)}
                    </td>
                </tr>
                """);
        }

        builder.AppendLine(
            $"""
                </tbody>
            </table>

            <p style="
                margin: 24px 0 0;
                color: #475569;
                font-size: 13px;">
                A listában szereplő eszközöket
                felvették, de nem adták le a riport
                készítésének idejéig.
            </p>

            <p style="
                margin: 8px 0 0;
                color: #64748b;
                font-size: 12px;">
                Készült:
                {generatedAtLocal:yyyy-MM-dd HH:mm}
            </p>

                </td>
            </tr>

            </table>
            </body>
            </html>
            """);

        return builder.ToString();
    }

    private void ValidateOptions()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("The end-of-day asset report is disabled.");
        }

        if (string.IsNullOrWhiteSpace(_options.TimeZoneId))
        {
            throw new InvalidOperationException("The report time zone identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.SubjectTemplate))
        {
            throw new InvalidOperationException("The report subject template is required.");
        }

        ValidateTimeZone();

        if (_options.TestMode)
        {
            ValidateAddresses(_options.TestRecipientAddressesTemplate,
                "At least one test recipient address is required when test mode is enabled.");

            return;
        }

        ValidateAddresses(_options.RecipientAddressesTemplate,
            "At least one recipient address template is required.");

        if (
            _options.RecipientAddressesTemplate.Any(
                template =>
                    !template.Contains(
                        "{0}",
                        StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Every recipient address template must contain the '{0}' placeholder.");
        }
    }

    private static void ValidateAddresses(
        string[]? addresses,
        string emptyCollectionErrorMessage)
    {
        if (addresses is null || addresses.Length == 0)
        {
            throw new InvalidOperationException(emptyCollectionErrorMessage);
        }

        if (addresses.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Recipient addresses cannot be empty.");
        }
    }

    private void ValidateTimeZone()
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(
                _options.TimeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException("The configured report time zone was not found.", exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException("The configured report time zone is invalid.", exception);
        }
    }

    private DateTime ConvertToLocalTime(DateTime utcDateTime)
    {
        var utcValue = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);

        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, timeZone);
    }

    private string FormatLocalTime(DateTime utcDateTime)
    {
        return ConvertToLocalTime(utcDateTime)
            .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }
}