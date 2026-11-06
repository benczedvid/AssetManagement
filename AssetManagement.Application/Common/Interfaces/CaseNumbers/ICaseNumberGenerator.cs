namespace AssetManagement.Application.Common.Interfaces.CaseNumbers;

public interface ICaseNumberGenerator
{
    Task<string> GenerateAsync(string prefix, DateTime occurredAtUtc, CancellationToken cancellationToken = default);
}