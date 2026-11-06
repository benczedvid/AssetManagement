using System.Globalization;
using System.Net;
using System.Text;
using AssetManagement.Application.Common.Email;
using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Application.Common.Interfaces.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssetManagement.Application.AssetMovements.AssetMovementReports;

public sealed class OverdueAssetReportService : IOverdueAssetReportService
{
    private readonly IUnreturnedEmployeeAssetReportRepository _reportRepository;

    private readonly IEmailSender _emailSender;
    private readonly OverdueAssetReportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OverdueAssetReportService> _logger;

    public OverdueAssetReportService(
        IUnreturnedEmployeeAssetReportRepository reportRepository,
        IEmailSender emailSender,
        IOptions<OverdueAssetReportOptions> options,
        TimeProvider timeProvider,
        ILogger<OverdueAssetReportService> logger)
    {
        ArgumentNullException.ThrowIfNull(reportRepository);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _reportRepository = reportRepository;
        _emailSender = emailSender;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<OverdueAssetReportResult> SendAsync(CancellationToken cancellationToken)
    {
        ValidateOptions();

        cancellationToken.ThrowIfCancellationRequested();

        var generatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var checkedOutBeforeUtc = generatedAtUtc.AddDays(-_options.MinimumUnreturnedDays);

        var reportRows =  await _reportRepository.GetOverdueUnreturnedEmployeeAssetsAsync(checkedOutBeforeUtc, cancellationToken);

        var storeGroups = reportRows
            .GroupBy(row => new
            {
                row.StoreId,
                row.StoreNumber,
                row.StoreName
            })
            .OrderBy(group => group.Key.StoreNumber)
            .Select(group => new StoreReportGroup(
                StoreId: group.Key.StoreId,
                StoreNumber: group.Key.StoreNumber,
                StoreName: group.Key.StoreName,
                Rows: group
                    .OrderBy(row => row.CheckedOutAtUtc)
                    .ThenBy(row => row.AssetName)
                    .ToArray()))
            .ToArray();

        if (reportRows.Count == 0)
        {
            _logger.LogInformation(
                "The overdue asset report contained no assets. " +
                "CheckedOutBeforeUtc: {CheckedOutBeforeUtc}, " +
                "GeneratedAtUtc: {GeneratedAtUtc}",
                checkedOutBeforeUtc,
                generatedAtUtc);

            return new OverdueAssetReportResult(
                StoreCount: 0,
                OverdueAssetCount: 0,
                EmailSent: false,
                GeneratedAtUtc: generatedAtUtc);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var message = CreateEmailMessage(
            storeGroups,
            checkedOutBeforeUtc,
            generatedAtUtc);

        try
        {
            await _emailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The overdue asset report could not be sent. " +
                "StoreCount: {StoreCount}, " +
                "OverdueAssetCount: {OverdueAssetCount}, " +
                "GeneratedAtUtc: {GeneratedAtUtc}",
                storeGroups.Length,
                reportRows.Count,
                generatedAtUtc);

            throw;
        }

        var result = new OverdueAssetReportResult(
            StoreCount: storeGroups.Length,
            OverdueAssetCount: reportRows.Count,
            EmailSent: true,
            GeneratedAtUtc: generatedAtUtc);

        _logger.LogInformation(
            "The overdue asset report was sent. " +
            "StoreCount: {StoreCount}, " +
            "OverdueAssetCount: {OverdueAssetCount}, " +
            "GeneratedAtUtc: {GeneratedAtUtc}",
            result.StoreCount,
            result.OverdueAssetCount,
            result.GeneratedAtUtc);

        return result;
    }

    private EmailMessage CreateEmailMessage(
        IReadOnlyCollection<StoreReportGroup> storeGroups,
        DateTime checkedOutBeforeUtc,
        DateTime generatedAtUtc)
    {
        var recipients = ResolveRecipientAddresses();
        var subject = _options.TestMode ? $"[TEST] {_options.Subject}" : _options.Subject;
        var textBody = CreateTextBody(storeGroups, checkedOutBeforeUtc, generatedAtUtc);
        var htmlBody = CreateHtmlBody(storeGroups, checkedOutBeforeUtc, generatedAtUtc);

        return new EmailMessage(
            ToRecipients: recipients,
            Subject: subject,
            TextBody: textBody,
            HtmlBody: htmlBody);
    }

    private string CreateTextBody(IReadOnlyCollection<StoreReportGroup> storeGroups, DateTime checkedOutBeforeUtc, DateTime generatedAtUtc)
    {
        var builder = new StringBuilder();
        builder.AppendLine("AssetManagement - 3 napja nem leadott eszközök");
        builder.AppendLine();
        builder.AppendLine($"Riport dátuma: {FormatLocalTime(generatedAtUtc)}");
        builder.AppendLine();
        builder.AppendLine($"Összesen: {storeGroups.Sum(group => group.Rows.Count)} eszköz");
        builder.AppendLine($"Érintett áruházak száma: {storeGroups.Count}");
        builder.AppendLine();

        foreach (var storeGroup in storeGroups)
        {
            builder.AppendLine($"{storeGroup.StoreNumber} - {storeGroup.StoreName} ({storeGroup.Rows.Count} eszköz)");

            foreach (var row in storeGroup.Rows)
            {
                builder.AppendLine($"{row.AssetName} | {FormatLocalTime(row.CheckedOutAtUtc)}");
            }

            builder.AppendLine();
        }

        builder.AppendLine(
            "A riport azokat az eszközöket tartalmazza, " +
            $"amelyeket legalább {_options.MinimumUnreturnedDays} napja " +
            "felvettek, de a riport készítésének időpontjáig " +
            "nem adtak le.");

        builder.AppendLine();

        builder.AppendLine($"Készült: {FormatLocalTime(generatedAtUtc)}");

        return builder.ToString();
    }

    private string CreateHtmlBody(IReadOnlyCollection<StoreReportGroup> storeGroups, DateTime checkedOutBeforeUtc, DateTime generatedAtUtc)
{
    var builder = new StringBuilder();

    var overdueAssetCount = storeGroups.Sum(group => group.Rows.Count);
    var generatedAtLocal = FormatLocalTime(generatedAtUtc);
    var checkedOutBeforeLocal = FormatLocalTime(checkedOutBeforeUtc);

    builder.AppendLine(
        """
        <!doctype html>
        <html lang="hu">
        <head>
            <meta charset="utf-8">
            <meta
                name="viewport"
                content="width=device-width, initial-scale=1">
            <title>
                Eszközkezelő - 3 napja nem leadott eszközök
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
                max-width: 900px;
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
                <strong style="
                    font-size: 20px;">
                    Eszközkezelő
                </strong>
            </td>
        </tr>
        """);

    builder.AppendLine(
        $"""
        <tr>
            <td style="
                padding: 24px;">

                <h1 style="
                    margin: 0;
                    color: #002b51;
                    font-size: 22px;">
                    Több, mint 3 napja nem leadott eszközök
                </h1>

                <p style="
                    margin: 8px 0 0;
                    color: #475569;
                    line-height: 1.5;">
                    A riport azokat az eszközöket tartalmazza,
                    amelyeket legalább
                    <strong>
                        {_options.MinimumUnreturnedDays} napja
                    </strong>
                    felvettek, de még nem adtak le a riport készítésének időpontjáig.</p>

                <table
                    role="presentation"
                    width="100%"
                    cellspacing="0"
                    cellpadding="0"
                    style="
                        margin: 20px 0;
                        border-collapse: collapse;">
                    <tr>
                        <td style="
                            width: 50%;
                            padding: 16px;
                            background-color: #eaf3f8;
                            border: 1px solid #cbd5e1;">
                            <div style="
                                color: #475569;
                                font-size: 16px;">
                                Érintett áruházak száma
                            </div>
                            <div style="
                                margin-top: 4px;
                                color: #002b51;
                                font-size: 24px;
                                font-weight: bold;">
                                {storeGroups.Count}
                            </div>
                        </td>

                        <td style="
                            width: 50%;
                            padding: 16px;
                            background-color: #eaf3f8;
                            border: 1px solid #cbd5e1;">
                            <div style="
                                color: #475569;
                                font-size: 16px;">
                                Nem leadott eszközök száma
                            </div>
                            <div style="
                                margin-top: 4px;
                                color: #002b51;
                                font-size: 24px;
                                font-weight: bold;">
                                {overdueAssetCount}
                            </div>
                        </td>
                    </tr>
                </table>

                <p style="
                    margin: 0 0 16px;
                    color: #64748b;
                    font-size: 13px;">
                    Szűrési határidő:
                    <strong>
                        {checkedOutBeforeLocal}
                    </strong>
                </p>
        """);

    foreach (var storeGroup in storeGroups)
    {
        var encodedStoreNumber = WebUtility.HtmlEncode(
            storeGroup.StoreNumber);

        var encodedStoreName = WebUtility.HtmlEncode(
            storeGroup.StoreName);

        builder.AppendLine(
            $"""
            <table
                role="presentation"
                width="100%"
                cellspacing="0"
                cellpadding="0"
                style="
                    width: 100%;
                    margin: 0 0 16px;
                    border-collapse: collapse;
                    border: 1px solid #cbd5e1;">

                <tr>
                    <td
                        colspan="2"
                        style="
                            padding: 14px 16px;
                            background-color: #002b51;
                            color: #ffffff;">
                        <strong style="
                            font-size: 20px;">
                            {encodedStoreNumber} - {encodedStoreName}
                        </strong>

                        <span style="
                            float: right;
                            font-size: 18px;">
                            {storeGroup.Rows.Count} eszköz
                        </span>
                    </td>
                </tr>

                <tr style="
                    background-color: #eaf3f8;
                    color: #002b51;">
                    <th style="
                        width: 65%;
                        padding: 8px 10px;
                        border: 1px solid #cbd5e1;
                        text-align: left;
                        font-size: 13px;">
                        Eszköz neve
                    </th>

                    <th style="
                        width: 35%;
                        padding: 8px 10px;
                        border: 1px solid #cbd5e1;
                        text-align: left;
                        font-size: 13px;">
                        Felvétel ideje
                    </th>
                </tr>
            """);

        foreach (var row in storeGroup.Rows)
        {
            var encodedAssetName = WebUtility.HtmlEncode(
                row.AssetName);

            var checkedOutAtLocal = FormatLocalTime(
                row.CheckedOutAtUtc);

            builder.AppendLine(
                $"""
                <tr>
                    <td style="
                        padding: 8px 10px;
                        border: 1px solid #cbd5e1;
                        color: #0f172a;
                        font-size: 14px;">
                        {encodedAssetName}
                    </td>

                    <td style="
                        padding: 8px 10px;
                        border: 1px solid #cbd5e1;
                        color: #0f172a;
                        font-size: 14px;
                        white-space: nowrap;">
                        {checkedOutAtLocal}
                    </td>
                </tr>
                """);
        }

        builder.AppendLine(
            """
            </table>
            """);
    }

    builder.AppendLine(
        $"""
                <p style="
                    margin: 8px 0 0;
                    color: #64748b;
                    font-size: 12px;">
                    Készült:
                    {generatedAtLocal}
                </p>
            </td>
        </tr>
        </table>
        </body>
        </html>
        """);

    return builder.ToString();
}

    private IReadOnlyCollection<string>ResolveRecipientAddresses()
    {
        if (_options.TestMode)
        {
            return
            [
                _options.TestRecipientAddress!.Trim()
            ];
        }

        return _options.RecipientAddresses
            .Select(address => address.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void ValidateOptions()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("The overdue asset report is disabled.");
        }

        if (_options.MinimumUnreturnedDays <= 0)
        {
            throw new InvalidOperationException(
                "The minimum unreturned day count must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(_options.TimeZoneId))
        {
            throw new InvalidOperationException("The report time zone identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.Subject))
        {
            throw new InvalidOperationException("The report subject is required.");
        }

        ValidateTimeZone();

        if (_options.TestMode)
        {
            if (string.IsNullOrWhiteSpace(_options.TestRecipientAddress))
            {
                throw new InvalidOperationException("The test recipient address is required when test mode is enabled.");
            }

            return;
        }

        if (_options.RecipientAddresses is null || _options.RecipientAddresses.Length == 0)
        {
            throw new InvalidOperationException("At least one report recipient address is required.");
        }

        if (_options.RecipientAddresses.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Report recipient addresses cannot be empty.");
        }
    }

    private void ValidateTimeZone()
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
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
        return ConvertToLocalTime(utcDateTime).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private sealed record StoreReportGroup(
        Guid StoreId,
        string StoreNumber,
        string StoreName,
        IReadOnlyCollection<UnreturnedEmployeeAssetReportRow> Rows);
}