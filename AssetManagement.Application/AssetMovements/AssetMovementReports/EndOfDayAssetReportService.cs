using System.Globalization;
using System.Net;
using System.Text;
using AssetManagement.Application.Common.Email;
using AssetManagement.Application.Common.Interfaces.AssetMovements;
using AssetManagement.Application.Common.Interfaces.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssetManagement.Application
    .AssetMovements
    .AssetMovementReports;

public sealed class EndOfDayAssetReportService: IEndOfDayAssetReportService
{
    private readonly IUnreturnedEmployeeAssetReportRepository _reportRepository;
    private readonly IEmailSender _emailSender;
    private readonly EndOfDayAssetReportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EndOfDayAssetReportService> _logger;

    public EndOfDayAssetReportService(
        IUnreturnedEmployeeAssetReportRepository reportRepository,
        IEmailSender emailSender,
        IOptions<EndOfDayAssetReportOptions> options,
        TimeProvider timeProvider,
        ILogger<EndOfDayAssetReportService> logger
        )
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

    public async Task<EndOfDayAssetReportResult>SendAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("The end-of-day asset report is disabled.");
        }

        var generatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var reportRows = await _reportRepository.GetUnreturnedEmployeeAssetsAsync(cancellationToken);
        var storeGroups = reportRows
                .GroupBy(
                    row =>
                        new
                        {
                            row.StoreId,
                            row.StoreNumber,
                            row.StoreName,
                        })
                .OrderBy(
                    group =>
                        group.Key.StoreNumber)
                .ToArray();

        var sentEmailCount = 0;
        var skippedStoreCount = 0;

        foreach (var storeGroup in storeGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rows = storeGroup
                    .OrderBy(
                        row =>
                            row.EmployeeFullName)
                    .ThenBy(
                        row =>
                            row.CheckedOutAtUtc)
                    .ToArray();

            if (rows.Length == 0 && !_options.SendEmptyReports)
            {
                skippedStoreCount++;
                continue;
            }

            var recipientAddress = ResolveRecipientAddress(storeGroup.Key.StoreNumber);

            var message = CreateEmailMessage(
                    storeNumber: storeGroup.Key.StoreNumber,
                    storeName: storeGroup.Key.StoreName,
                    recipientAddress: recipientAddress,
                    rows: rows,
                    generatedAtUtc: generatedAtUtc);

            try
            {
                await _emailSender.SendAsync(message, cancellationToken);

                sentEmailCount++;

                _logger.LogInformation(
                    "The end-of-day asset report was sent. " +
                    "StoreId: {StoreId}, " +
                    "StoreNumber: {StoreNumber}, " +
                    "Recipient: {Recipient}, " +
                    "UnreturnedAssetCount: " +
                    "{UnreturnedAssetCount}",
                    storeGroup.Key.StoreId,
                    storeGroup.Key.StoreNumber,
                    recipientAddress,
                    rows.Length);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The end-of-day asset report " +
                    "could not be sent. " +
                    "StoreId: {StoreId}, " +
                    "StoreNumber: {StoreNumber}, " +
                    "Recipient: {Recipient}",
                    storeGroup.Key.StoreId,
                    storeGroup.Key.StoreNumber,
                    recipientAddress);

                throw;
            }
        }

        var result =
            new EndOfDayAssetReportResult(
                StoreCount: storeGroups.Length,
                SentEmailCount: sentEmailCount,
                SkippedStoreCount: skippedStoreCount,
                UnreturnedAssetCount: reportRows.Count,
                GeneratedAtUtc: generatedAtUtc);

        _logger.LogInformation(
            "The end-of-day asset report run completed. " +
            "StoreCount: {StoreCount}, " +
            "SentEmailCount: {SentEmailCount}, " +
            "SkippedStoreCount: {SkippedStoreCount}, " +
            "UnreturnedAssetCount: " +
            "{UnreturnedAssetCount}, " +
            "GeneratedAtUtc: {GeneratedAtUtc}",
            result.StoreCount,
            result.SentEmailCount,
            result.SkippedStoreCount,
            result.UnreturnedAssetCount,
            result.GeneratedAtUtc);

        return result;
    }

    private string CreateRecipientAddress(string storeNumber)
    {
        if (string.IsNullOrWhiteSpace(storeNumber))
        {
            throw new InvalidOperationException("The store number is required.");
        }

        var normalizedStoreNumber = storeNumber.Trim();

        if (
            normalizedStoreNumber.Length != 3 ||
            !normalizedStoreNumber.All(char.IsDigit))
        {
            throw new InvalidOperationException("The store number must contain exactly three digits.");
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            _options.RecipientAddressTemplate,
            normalizedStoreNumber);
    }
    private string ResolveRecipientAddress(string storeNumber)
    {
        if (_options.TestMode)
        {
            if (string.IsNullOrWhiteSpace(_options.TestRecipientAddress))
            {
                throw new InvalidOperationException("The test recipient address is required when test mode is enabled.");
            }

            return _options.TestRecipientAddress.Trim();
        }

        if (string.IsNullOrWhiteSpace(storeNumber))
        {
            throw new InvalidOperationException("The store number is required.");
        }

        var normalizedStoreNumber = storeNumber.Trim();

        if (
            normalizedStoreNumber.Length != 3 ||
            !normalizedStoreNumber.All(
                char.IsDigit))
        {
            throw new InvalidOperationException("The store number must contain exactly three digits.");
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            _options.RecipientAddressTemplate,
            normalizedStoreNumber);
    }

    private EmailMessage CreateEmailMessage(
        string storeNumber,
        string storeName,
        string recipientAddress,
        IReadOnlyCollection<UnreturnedEmployeeAssetReportRow> rows,
        DateTime generatedAtUtc)
    {
        var storeDisplayName = $"{storeNumber} - {storeName}";
        var reportSubject = string.Format(CultureInfo.InvariantCulture, _options.SubjectTemplate, storeDisplayName);
        var subject = _options.TestMode ? $"[TEST] {reportSubject}" : reportSubject;
        var generatedAtLocal = ConvertToLocalTime(generatedAtUtc);
        var textBody = CreateTextBody(storeDisplayName, rows, generatedAtLocal);
        var htmlBody = CreateHtmlBody(storeDisplayName, rows, generatedAtLocal);

        return new EmailMessage(
            ToRecipients: [recipientAddress],
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

        builder.AppendLine("AssetManagement Napi riport");
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
        builder.AppendLine("A listában szereplő eszközöket felvették, de nem adták le a riport készítésének időpontjáig.");
        builder.AppendLine();
        builder.AppendLine($"Készült: " + $"{generatedAtLocal:yyyy-MM-dd HH:mm}");

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
            <html lang="en">
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
                A listában szereplő eszközöket felvették, de nem adták le a riport készítésének idejéig.
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

    private DateTime ConvertToLocalTime(DateTime utcDateTime)
    {
        var utcValue = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, timeZone);
    }

    private string FormatLocalTime(DateTime utcDateTime)
    {
        return ConvertToLocalTime(
                utcDateTime)
            .ToString(
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture);
    }
}