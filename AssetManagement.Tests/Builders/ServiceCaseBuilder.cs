using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Tests.Builders;

public sealed class ServiceCaseBuilder
{
    private string? _caseNumber = "SC-2026-0001";
    private Guid _assetId = Guid.NewGuid();
    private ServiceCaseStatus _initialStatus = ServiceCaseStatus.UnderCentralInspection;
    private ServiceIssueType _issueType = ServiceIssueType.DisplayIssue;
    private string? _otherIssueDescription;
    private string? _description = "The asset display does not work.";
    private Guid? _originStoreId;
    private Guid _createdByUserId = Guid.NewGuid();
    private string? _createdByUserName = "Test User";
    private DateTime _createdAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    public ServiceCaseBuilder WithCaseNumber(string? caseNumber)
    {
        _caseNumber = caseNumber;
        return this;
    }

    public ServiceCaseBuilder WithAssetId(Guid assetId)
    {
        _assetId = assetId;
        return this;
    }

    public ServiceCaseBuilder WithInitialStatus(ServiceCaseStatus initialStatus)
    {
        _initialStatus = initialStatus;
        return this;
    }

    public ServiceCaseBuilder WithIssueType(ServiceIssueType issueType)
    {
        _issueType = issueType;
        return this;
    }

    public ServiceCaseBuilder WithOtherIssueDescription(string? otherIssueDescription)
    {
        _otherIssueDescription = otherIssueDescription;
        return this;
    }

    public ServiceCaseBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public ServiceCaseBuilder WithOriginStoreId(Guid? originStoreId)
    {
        _originStoreId = originStoreId;
        return this;
    }

    public ServiceCaseBuilder WithCreatedByUserId(Guid createdByUserId)
    {
        _createdByUserId = createdByUserId;
        return this;
    }

    public ServiceCaseBuilder WithCreatedByUserName(string? createdByUserName)
    {
        _createdByUserName = createdByUserName;
        return this;
    }

    public ServiceCaseBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    public ServiceCaseBuilder AsOpenedFromStore(Guid originStoreId)
    {
        _initialStatus = ServiceCaseStatus.AwaitingCentralReceipt;
        _originStoreId = originStoreId;
        return this;
    }

    public ServiceCaseBuilder AsOpenedAtHeadquarters()
    {
        _initialStatus = ServiceCaseStatus.UnderCentralInspection;
        _originStoreId = null;
        return this;
    }

    public ServiceCase Build()
    {
        return ServiceCase.Create(
            caseNumber: _caseNumber!,
            assetId: _assetId,
            initialStatus: _initialStatus,
            issueType: _issueType,
            otherIssueDescription: _otherIssueDescription,
            description: _description!,
            originStoreId: _originStoreId,
            createdByUserId: _createdByUserId,
            createdByUserName: _createdByUserName!,
            createdAtUtc: _createdAtUtc);
    }
}