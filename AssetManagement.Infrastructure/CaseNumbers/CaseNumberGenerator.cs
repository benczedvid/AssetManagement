using AssetManagement.Application.Common.Interfaces.CaseNumbers;
using AssetManagement.Domain.Entities.CaseNumbers;
using AssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.CaseNumbers;

public sealed class CaseNumberGenerator : ICaseNumberGenerator
{
    private const int MaximumRetryCount = 3;

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public CaseNumberGenerator(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        _dbContextFactory = dbContextFactory;
    }

    public async Task<string> GenerateAsync(string prefix, DateTime occurredAtUtc, CancellationToken cancellationToken = default)
    {
        ValidatePrefix(prefix);
        ValidateUtcDateTime(occurredAtUtc);

        var normalizedPrefix = prefix.Trim().ToUpperInvariant();

        var year = occurredAtUtc.Year;
        Exception? lastException = null;

        for (var attempt = 1;
            attempt <= MaximumRetryCount;
            attempt++)
        {
            await using var appDbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            try
            {
                var sequence = await appDbContext.CaseNumberSequences
                        .FirstOrDefaultAsync(
                            currentSequence =>
                                currentSequence.Prefix == normalizedPrefix && currentSequence.Year == year,
                            cancellationToken);

                if (sequence is null)
                {
                    sequence = CaseNumberSequence.Create(normalizedPrefix, year);

                    await appDbContext.CaseNumberSequences.AddAsync(sequence, cancellationToken);
                }

                var nextNumber = sequence.GetNextNumber();

                await appDbContext.SaveChangesAsync(cancellationToken);

                return FormatCaseNumber(normalizedPrefix, year, nextNumber);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                lastException = exception;
            }
            catch (DbUpdateException exception)
            {
                lastException = exception;
            }
        }

        throw new InvalidOperationException("A unique case number could not be generated after multiple attempts.", lastException);
    }

    private static string FormatCaseNumber(string prefix, int year, int number)
    {
        return $"{prefix}-{year:D4}-{number:D6}";
    }

    private static void ValidatePrefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var normalizedPrefix = prefix.Trim();

        if (normalizedPrefix.Length > 10)
        {
            throw new ArgumentException("The case number prefix cannot exceed 10 characters.", nameof(prefix));
        }

        if (!normalizedPrefix.All(char.IsLetterOrDigit))
        {
            throw new ArgumentException("The case number prefix can only contain letters and digits.", nameof(prefix));
        }
    }

    private static void ValidateUtcDateTime(DateTime occurredAtUtc)
    {
        if (occurredAtUtc == default)
        {
            throw new ArgumentException("The occurrence date cannot be empty.", nameof(occurredAtUtc));
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The occurrence date must be in UTC.", nameof(occurredAtUtc));
        }
    }
}