using AssetManagement.Domain.Entities.CaseNumbers;

namespace AssetManagement.Tests.Builders;

public sealed class CaseNumberSequenceBuilder
{
    private string? _prefix = "SC";
    private int _year = 2026;

    public CaseNumberSequenceBuilder WithPrefix(string? prefix)
    {
        _prefix = prefix;
        return this;
    }

    public CaseNumberSequenceBuilder WithYear(int year)
    {
        _year = year;
        return this;
    }

    public CaseNumberSequence Build()
    {
        return CaseNumberSequence.Create(
            prefix: _prefix!,
            year: _year);
    }
}