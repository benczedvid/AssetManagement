namespace AssetManagement.Domain.Entities.CaseNumbers;

public sealed class CaseNumberSequence
{
    public string Prefix { get; private set; } = null!;
    public int Year { get; private set; }
    public int LastNumber { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private CaseNumberSequence()
    {
    }

    private CaseNumberSequence(string prefix, int year)
    {
        Prefix = prefix;
        Year = year;
        LastNumber = 0;
    }

    public static CaseNumberSequence Create(string prefix, int year)
    {
        ValidatePrefix(prefix);
        ValidateYear(year);

        return new CaseNumberSequence(prefix: prefix.Trim().ToUpperInvariant(), year: year);
    }

    public int GetNextNumber()
    {
        if (LastNumber == int.MaxValue)
        {
            throw new InvalidOperationException("The case number sequence has reached its maximum value.");
        }

        LastNumber++;

        return LastNumber;
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

    private static void ValidateYear(int year)
    {
        if (year < 2000 || year > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "The case number year must be between 2000 and 9999.");
        }
    }
}