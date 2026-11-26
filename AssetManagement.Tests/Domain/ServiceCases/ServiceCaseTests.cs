using System;
using System.Reflection;
using AssetManagement.Domain.Entities.ServiceCases;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AssetManagement.Tests.Domain.Entities.ServiceCases;

[TestClass]
public sealed class ServiceCaseTests
{
    private static readonly Guid AssetId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid VendorId = Guid.NewGuid();
    private static readonly DateTime CreatedAt = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Create_AtHeadquarters_SetsInitialProperties()
    {
        var serviceCase = CreateCase();

        Assert.AreNotEqual(Guid.Empty, serviceCase.Id);
        Assert.AreEqual("SC-2026-0001", serviceCase.CaseNumber);
        Assert.AreEqual(AssetId, serviceCase.AssetId);
        Assert.AreEqual(ServiceCaseStatus.UnderCentralInspection, serviceCase.Status);
        Assert.AreEqual(ServiceIssueType.DisplayIssue, serviceCase.IssueType);
        Assert.AreEqual("Display does not work.", serviceCase.Description);
        Assert.AreEqual(UserId, serviceCase.CreatedByUserId);
        Assert.AreEqual("Test User", serviceCase.CreatedByUserName);
        Assert.AreEqual(CreatedAt, serviceCase.CreatedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, serviceCase.CreatedAtUtc.Kind);
        Assert.IsTrue(serviceCase.IsActive);
        Assert.IsNull(serviceCase.OtherIssueDescription);
        Assert.IsNull(serviceCase.OriginStoreId);
        Assert.IsNull(serviceCase.VendorId);
        Assert.IsNull(serviceCase.ClosedByUserId);
        Assert.IsNull(serviceCase.ClosedAtUtc);
    }

    [TestMethod]
    public void Create_FromStore_PreservesOriginStore()
    {
        var serviceCase = CreateCase(
            initialStatus: ServiceCaseStatus.AwaitingCentralReceipt,
            originStoreId: StoreId);

        Assert.AreEqual(ServiceCaseStatus.AwaitingCentralReceipt, serviceCase.Status);
        Assert.AreEqual(StoreId, serviceCase.OriginStoreId);
    }

    [TestMethod]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        Assert.AreNotEqual(CreateCase().Id, CreateCase().Id);
    }

    [TestMethod]
    public void Create_WithPaddedText_TrimsValues()
    {
        var serviceCase = CreateCase(
            caseNumber: "  SC-2026-0001  ",
            description: "  Display does not work.  ",
            createdByUserName: "  Test User  ",
            issueType: ServiceIssueType.Other,
            otherIssueDescription: "  Custom issue  ");

        Assert.AreEqual("SC-2026-0001", serviceCase.CaseNumber);
        Assert.AreEqual("Display does not work.", serviceCase.Description);
        Assert.AreEqual("Test User", serviceCase.CreatedByUserName);
        Assert.AreEqual("Custom issue", serviceCase.OtherIssueDescription);
    }

    [TestMethod]
    public void Create_WithMaximumTextLengths_AcceptsValues()
    {
        var serviceCase = CreateCase(
            caseNumber: new string('C', 50),
            description: new string('D', 2000),
            createdByUserName: new string('U', 250),
            issueType: ServiceIssueType.Other,
            otherIssueDescription: new string('O', 250));

        Assert.AreEqual(50, serviceCase.CaseNumber.Length);
        Assert.AreEqual(2000, serviceCase.Description.Length);
        Assert.AreEqual(250, serviceCase.CreatedByUserName.Length);
        Assert.AreEqual(250, serviceCase.OtherIssueDescription!.Length);
    }

    [TestMethod]
    [DataRow("caseNumber")]
    [DataRow("description")]
    [DataRow("createdByUserName")]
    public void Create_WithInvalidRequiredText_Throws(string parameter)
    {
        foreach (var value in new string?[] { null, "", " ", "\t\r\n" })
        {
            Action action = () => CreateCase(
                caseNumber: parameter == "caseNumber" ? value! : "SC-2026-0001",
                description: parameter == "description" ? value! : "Display does not work.",
                createdByUserName: parameter == "createdByUserName" ? value! : "Test User");

            if (value is null)
            {
                AssertParameter<ArgumentNullException>(action, parameter);
            }
            else
            {
                AssertParameter<ArgumentException>(action, parameter);
            }
        }
    }

    [TestMethod]
    [DataRow("caseNumber", 51)]
    [DataRow("description", 2001)]
    [DataRow("createdByUserName", 251)]
    public void Create_WithTooLongText_Throws(string parameter, int length)
    {
        var value = new string('X', length);

        AssertParameter<ArgumentException>(
            () => CreateCase(
                caseNumber: parameter == "caseNumber" ? value : "SC-2026-0001",
                description: parameter == "description" ? value : "Display does not work.",
                createdByUserName: parameter == "createdByUserName" ? value : "Test User"),
            parameter);
    }

    [TestMethod]
    [DataRow("assetId")]
    [DataRow("createdByUserId")]
    public void Create_WithEmptyIdentifier_Throws(string parameter)
    {
        AssertParameter<ArgumentException>(
            () => CreateCase(
                assetId: parameter == "assetId" ? Guid.Empty : AssetId,
                createdByUserId: parameter == "createdByUserId" ? Guid.Empty : UserId),
            parameter);
    }

    [TestMethod]
    public void Create_WithInvalidInitialStatus_Throws()
    {
        foreach (var status in Enum.GetValues<ServiceCaseStatus>())
        {
            if (status is ServiceCaseStatus.AwaitingCentralReceipt or ServiceCaseStatus.UnderCentralInspection)
            {
                continue;
            }

            Action action = () => CreateCase(initialStatus: status);

            if (status == ServiceCaseStatus.Unknown)
            {
                AssertParameter<ArgumentOutOfRangeException>(action, "status");
            }
            else
            {
                AssertParameter<ArgumentException>(action, "status");
            }
        }

        AssertParameter<ArgumentOutOfRangeException>(() => CreateCase(initialStatus: (ServiceCaseStatus)999), "status");
    }

    [TestMethod]
    public void Create_WithInvalidIssueType_Throws()
    {
        foreach (var issueType in new[]
                 {
                     ServiceIssueType.Unknown,
                     (ServiceIssueType)999
                 })
        {
            AssertParameter<ArgumentOutOfRangeException>(() => CreateCase(issueType: issueType), "issueType");
        }
    }

    [TestMethod]
    public void Create_WithDefinedIssueTypes_AcceptsValues()
    {
        foreach (var issueType in Enum.GetValues<ServiceIssueType>())
        {
            if (issueType == ServiceIssueType.Unknown)
            {
                continue;
            }

            var serviceCase = CreateCase(
                issueType: issueType,
                otherIssueDescription: issueType == ServiceIssueType.Other ? "Custom issue" : null);

            Assert.AreEqual(issueType, serviceCase.IssueType);
        }
    }

    [TestMethod]
    public void Create_WithInvalidOriginStore_Throws()
    {
        AssertParameter<ArgumentException>(() => CreateCase(originStoreId: Guid.Empty), "originStoreId");
        AssertParameter<ArgumentException>(() => CreateCase(originStoreId: StoreId), "originStoreId");
        AssertParameter<ArgumentException>(() => CreateCase(initialStatus: ServiceCaseStatus.AwaitingCentralReceipt), "originStoreId");
    }

    [TestMethod]
    public void Create_WithInvalidDate_Throws()
    {
        foreach (var date in new[]
                 {
                     default,
                     DateTime.SpecifyKind(CreatedAt, DateTimeKind.Local),
                     DateTime.SpecifyKind(CreatedAt, DateTimeKind.Unspecified)
                 })
        {
            AssertParameter<ArgumentException>(() => CreateCase(createdAtUtc: date), "createdAtUtc");
        }
    }

    [TestMethod]
    public void Create_WithOtherIssue_RequiresValidDescription()
    {
        foreach (var value in new string?[]
                 {
                     null, "", " ", "\t\r\n", new string('X', 251)
                 })
        {
            AssertParameter<ArgumentException>(() => CreateCase(issueType: ServiceIssueType.Other, otherIssueDescription: value), "otherIssueDescription");
        }
    }

    [TestMethod]
    public void Create_WithNonOtherIssue_RejectsOtherDescription()
    {
        AssertParameter<ArgumentException>(() => CreateCase(otherIssueDescription: "Custom issue"), "otherIssueDescription");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void Create_WithNonOtherIssue_NormalizesEmptyOtherDescription(string? value)
    {
        Assert.IsNull(CreateCase(otherIssueDescription: value).OtherIssueDescription);
    }

    [TestMethod]
    [DataRow("ReceiveAtHeadquarters", ServiceCaseStatus.AwaitingCentralReceipt, ServiceCaseStatus.UnderCentralInspection)]
    [DataRow("RequireExternalService", ServiceCaseStatus.UnderCentralInspection, ServiceCaseStatus.AwaitingExternalServiceDispatch)]
    [DataRow("DispatchToExternalService", ServiceCaseStatus.AwaitingExternalServiceDispatch, ServiceCaseStatus.SentToExternalService)]
    [DataRow("HandOverToExternalService", ServiceCaseStatus.AwaitingExternalServiceDispatch, ServiceCaseStatus.InExternalService)]
    [DataRow("ConfirmReceivedByExternalService", ServiceCaseStatus.SentToExternalService, ServiceCaseStatus.InExternalService)]
    [DataRow("ReceiveFromExternalService", ServiceCaseStatus.InExternalService, ServiceCaseStatus.AwaitingReturn)]
    [DataRow("ReturnFromExternalService", ServiceCaseStatus.InExternalService, ServiceCaseStatus.AwaitingReturn)]
    [DataRow("DispatchToOriginStore", ServiceCaseStatus.AwaitingReturn, ServiceCaseStatus.ReturningToStore)]
    [DataRow("ReceiveAtOriginStore", ServiceCaseStatus.ReturningToStore, ServiceCaseStatus.Resolved)]
    [DataRow("CompleteRepairAtHeadquarters", ServiceCaseStatus.UnderCentralInspection, ServiceCaseStatus.Resolved)]
    [DataRow("ResolveAfterExternalServiceReturn", ServiceCaseStatus.AwaitingReturn, ServiceCaseStatus.Resolved)]
    public void LifecycleOperation_ValidatesSourceAndSetsTargetStatus(string operation, ServiceCaseStatus source, ServiceCaseStatus target)
    {
        foreach (var status in Enum.GetValues<ServiceCaseStatus>())
        {
            var serviceCase = CreateCase();
            SetProperty(serviceCase, nameof(ServiceCase.Status), status);
            SetProperty(serviceCase, nameof(ServiceCase.VendorId), VendorId);

            if (operation is "DispatchToOriginStore" or "ReceiveAtOriginStore")
            {
                SetProperty(serviceCase, nameof(ServiceCase.OriginStoreId), StoreId);
            }

            if (status == source)
            {
                Invoke(serviceCase, operation);
                Assert.AreEqual(target, serviceCase.Status, operation);

                if (operation == "RequireExternalService")
                {
                    Assert.AreEqual(VendorId, serviceCase.VendorId);
                }
            }
            else
            {
                Assert.ThrowsExactly<InvalidOperationException>(() => Invoke(serviceCase, operation));
                Assert.AreEqual(status, serviceCase.Status, operation);
            }
        }
    }

    [TestMethod]
    public void RequireExternalService_WithEmptyVendor_Throws()
    {
        var serviceCase = CreateCase();

        AssertParameter<ArgumentException>(() => serviceCase.RequireExternalService(Guid.Empty), "vendorId");

        Assert.IsNull(serviceCase.VendorId);
        Assert.AreEqual(ServiceCaseStatus.UnderCentralInspection, serviceCase.Status);
    }

    [TestMethod]
    [DataRow("DispatchToExternalService", ServiceCaseStatus.AwaitingExternalServiceDispatch)]
    [DataRow("HandOverToExternalService", ServiceCaseStatus.AwaitingExternalServiceDispatch)]
    [DataRow("ConfirmReceivedByExternalService", ServiceCaseStatus.SentToExternalService)]
    [DataRow("ReceiveFromExternalService", ServiceCaseStatus.InExternalService)]
    [DataRow("ResolveAfterExternalServiceReturn", ServiceCaseStatus.AwaitingReturn)]
    public void ExternalOperation_WithInvalidVendor_Throws(string operation, ServiceCaseStatus status)
    {
        foreach (var vendor in new Guid?[] { null, Guid.Empty })
        {
            var serviceCase = CreateCase();
            SetProperty(serviceCase, nameof(ServiceCase.Status), status);
            SetProperty(serviceCase, nameof(ServiceCase.VendorId), vendor);

            Assert.ThrowsExactly<InvalidOperationException>(() => Invoke(serviceCase, operation));
            Assert.AreEqual(status, serviceCase.Status);
        }
    }

    [TestMethod]
    [DataRow("DispatchToOriginStore", ServiceCaseStatus.AwaitingReturn)]
    [DataRow("ReceiveAtOriginStore", ServiceCaseStatus.ReturningToStore)]
    public void StoreOperation_WithInvalidOriginStore_Throws(string operation, ServiceCaseStatus status)
    {
        foreach (var store in new Guid?[] { null, Guid.Empty })
        {
            var serviceCase = CreateCase();
            SetProperty(serviceCase, nameof(ServiceCase.Status), status);
            SetProperty(serviceCase, nameof(ServiceCase.OriginStoreId), store);

            Assert.ThrowsExactly<InvalidOperationException>(() => Invoke(serviceCase, operation));
            Assert.AreEqual(status, serviceCase.Status);
        }
    }

    [TestMethod]
    public void CompleteRepairAtHeadquarters_WithOriginStore_AwaitsReturn()
    {
        var serviceCase = CreateCase(
            initialStatus: ServiceCaseStatus.AwaitingCentralReceipt,
            originStoreId: StoreId);

        serviceCase.ReceiveAtHeadquarters();
        serviceCase.CompleteRepairAtHeadquarters();

        Assert.AreEqual(ServiceCaseStatus.AwaitingReturn, serviceCase.Status);
    }

    [TestMethod]
    public void CompleteRepairAtHeadquarters_WithEmptyOriginStore_Throws()
    {
        var serviceCase = CreateCase();
        SetProperty(serviceCase, nameof(ServiceCase.OriginStoreId), Guid.Empty);

        Assert.ThrowsExactly<InvalidOperationException>(serviceCase.CompleteRepairAtHeadquarters);

        Assert.AreEqual(ServiceCaseStatus.UnderCentralInspection, serviceCase.Status);
    }

    [TestMethod]
    public void ResolveAfterExternalServiceReturn_WithOriginStore_Throws()
    {
        var serviceCase = CreateCase();
        SetProperty(serviceCase, nameof(ServiceCase.Status), ServiceCaseStatus.AwaitingReturn);
        SetProperty(serviceCase, nameof(ServiceCase.VendorId), VendorId);
        SetProperty(serviceCase, nameof(ServiceCase.OriginStoreId), StoreId);

        Assert.ThrowsExactly<InvalidOperationException>(serviceCase.ResolveAfterExternalServiceReturn);
        Assert.AreEqual(ServiceCaseStatus.AwaitingReturn, serviceCase.Status);
    }

    [TestMethod]
    public void ResolveByAssetDisposal_ValidatesAllowedStatuses()
    {
        foreach (var status in Enum.GetValues<ServiceCaseStatus>())
        {
            var serviceCase = CreateCase();
            SetProperty(serviceCase, nameof(ServiceCase.Status), status);
            SetProperty(serviceCase, nameof(ServiceCase.VendorId), VendorId);

            var allowed = status is
                ServiceCaseStatus.UnderCentralInspection or
                ServiceCaseStatus.AwaitingExternalServiceDispatch or
                ServiceCaseStatus.InExternalService or
                ServiceCaseStatus.AwaitingReturn;

            if (allowed)
            {
                serviceCase.ResolveByAssetDisposal();

                Assert.AreEqual(ServiceCaseStatus.Resolved, serviceCase.Status);
                Assert.IsTrue(serviceCase.IsActive);
                Assert.IsNull(serviceCase.ClosedAtUtc);
            }
            else
            {
                Assert.ThrowsExactly<InvalidOperationException>(serviceCase.ResolveByAssetDisposal);
                Assert.AreEqual(status, serviceCase.Status);
            }
        }
    }

    [TestMethod]
    [DataRow(ServiceCaseStatus.AwaitingExternalServiceDispatch)]
    [DataRow(ServiceCaseStatus.InExternalService)]
    public void ResolveByAssetDisposal_AtExternalStage_RequiresVendor(ServiceCaseStatus status)
    {
        foreach (var vendor in new Guid?[] { null, Guid.Empty })
        {
            var serviceCase = CreateCase();
            SetProperty(serviceCase, nameof(ServiceCase.Status), status);
            SetProperty(serviceCase, nameof(ServiceCase.VendorId), vendor);

            Assert.ThrowsExactly<InvalidOperationException>(serviceCase.ResolveByAssetDisposal);
            Assert.AreEqual(status, serviceCase.Status);
        }
    }

    [TestMethod]
    public void Close_WithValidArguments_SetsClosingFields()
    {
        var serviceCase = CreateCase();
        serviceCase.CompleteRepairAtHeadquarters();
        var closedAt = CreatedAt.AddHours(1);

        serviceCase.Close(UserId, closedAt);

        Assert.AreEqual(ServiceCaseStatus.Closed, serviceCase.Status);
        Assert.AreEqual(UserId, serviceCase.ClosedByUserId);
        Assert.AreEqual(closedAt, serviceCase.ClosedAtUtc);
        Assert.IsFalse(serviceCase.IsActive);
    }

    [TestMethod]
    public void Close_AtCreationTime_IsAllowed()
    {
        var serviceCase = CreateCase();
        serviceCase.CompleteRepairAtHeadquarters();

        serviceCase.Close(UserId, CreatedAt);

        Assert.AreEqual(CreatedAt, serviceCase.ClosedAtUtc);
    }

    [TestMethod]
    public void Close_WhenNotResolved_Throws()
    {
        foreach (var status in Enum.GetValues<ServiceCaseStatus>())
        {
            if (status == ServiceCaseStatus.Resolved)
            {
                continue;
            }

            var serviceCase = CreateCase();
            SetProperty(serviceCase, nameof(ServiceCase.Status), status);

            Assert.ThrowsExactly<InvalidOperationException>(() => serviceCase.Close(UserId, CreatedAt));
            Assert.AreEqual(status, serviceCase.Status);
            Assert.IsNull(serviceCase.ClosedAtUtc);
            Assert.IsNull(serviceCase.ClosedByUserId);
        }
    }

    [TestMethod]
    public void Close_WithInvalidArguments_PreservesResolvedState()
    {
        var serviceCase = CreateCase();
        serviceCase.CompleteRepairAtHeadquarters();

        AssertParameter<ArgumentException>(() => serviceCase.Close(Guid.Empty, CreatedAt), "closedByUserId");

        foreach (var date in new[]
                 {
                     default,
                     CreatedAt.AddTicks(-1),
                     DateTime.SpecifyKind(CreatedAt, DateTimeKind.Local),
                     DateTime.SpecifyKind(CreatedAt, DateTimeKind.Unspecified)
                 })
        {
            AssertParameter<ArgumentException>(() => serviceCase.Close(UserId, date),"closedAtUtc");
        }

        Assert.AreEqual(ServiceCaseStatus.Resolved, serviceCase.Status);
        Assert.IsNull(serviceCase.ClosedAtUtc);
        Assert.IsNull(serviceCase.ClosedByUserId);
    }

    [TestMethod]
    public void IsActive_IsFalseOnlyForClosedAndCancelled()
    {
        foreach (var status in Enum.GetValues<ServiceCaseStatus>())
        {
            var serviceCase = CreateCase();
            SetProperty(serviceCase, nameof(ServiceCase.Status), status);

            Assert.AreEqual(status is not (ServiceCaseStatus.Closed or ServiceCaseStatus.Cancelled), serviceCase.IsActive);
        }
    }

    private static ServiceCase CreateCase(
        string caseNumber = "SC-2026-0001",
        Guid? assetId = null,
        ServiceCaseStatus initialStatus = ServiceCaseStatus.UnderCentralInspection,
        ServiceIssueType issueType = ServiceIssueType.DisplayIssue,
        string? otherIssueDescription = null,
        string description = "Display does not work.",
        Guid? originStoreId = null,
        Guid? createdByUserId = null,
        string createdByUserName = "Test User",
        DateTime? createdAtUtc = null)
    {
        return ServiceCase.Create(
            caseNumber,
            assetId ?? AssetId,
            initialStatus,
            issueType,
            otherIssueDescription,
            description,
            originStoreId,
            createdByUserId ?? UserId,
            createdByUserName,
            createdAtUtc ?? CreatedAt);
    }

    private static void Invoke(ServiceCase serviceCase, string operation)
    {
        switch (operation)
        {
            case "ReceiveAtHeadquarters":
                serviceCase.ReceiveAtHeadquarters();
                break;
            case "RequireExternalService":
                serviceCase.RequireExternalService(VendorId);
                break;
            case "DispatchToExternalService":
                serviceCase.DispatchToExternalService();
                break;
            case "HandOverToExternalService":
                serviceCase.HandOverToExternalService();
                break;
            case "ConfirmReceivedByExternalService":
                serviceCase.ConfirmReceivedByExternalService();
                break;
            case "ReceiveFromExternalService":
                serviceCase.ReceiveFromExternalService();
                break;
            case "ReturnFromExternalService":
                serviceCase.ReturnFromExternalService();
                break;
            case "DispatchToOriginStore":
                serviceCase.DispatchToOriginStore();
                break;
            case "ReceiveAtOriginStore":
                serviceCase.ReceiveAtOriginStore();
                break;
            case "CompleteRepairAtHeadquarters":
                serviceCase.CompleteRepairAtHeadquarters();
                break;
            case "ResolveAfterExternalServiceReturn":
                serviceCase.ResolveAfterExternalServiceReturn();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static void SetProperty(ServiceCase serviceCase, string name, object? value)
    {
        var property = typeof(ServiceCase).GetProperty( name, BindingFlags.Instance | BindingFlags.Public);

        Assert.IsNotNull(property);

        var setter = property.GetSetMethod(nonPublic: true);
        Assert.IsNotNull(setter);

        setter.Invoke(serviceCase, new object?[] { value });
    }

    private static void AssertParameter<TException>(Action action, string parameter) where TException : ArgumentException
    {
        var exception = Assert.ThrowsExactly<TException>(action);
        Assert.AreEqual(parameter, exception.ParamName);
    }
}