namespace AssetManagement.Domain.Entities.ServiceCases
{
    public sealed class ServiceCase
    {
        public Guid Id { get; private set; }
        public string CaseNumber { get; private set; } = null!;
        public Guid AssetId { get; private set; }
        public ServiceCaseStatus Status { get; private set; }
        public ServiceIssueType IssueType { get; private set; }
        public string? OtherIssueDescription { get; private set; }
        public string Description { get; private set; } = null!;
        public Guid? OriginStoreId { get; private set; }
        public Guid? VendorId { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public string CreatedByUserName { get; private set; } = null!;
        public DateTime CreatedAtUtc { get; private set; }
        public Guid? ClosedByUserId { get; private set; }
        public DateTime? ClosedAtUtc { get; private set; }
        public bool IsActive => Status != ServiceCaseStatus.Closed && Status != ServiceCaseStatus.Cancelled;

        private ServiceCase() { }
        private ServiceCase(
            Guid id,
            string caseNumber,
            Guid assetId,
            ServiceCaseStatus status,
            ServiceIssueType issueType,
            string? otherIssueDescription,
            string description,
            Guid? originStoreId,
            Guid createdByUserId,
            string createdByUserName,
            DateTime createdAtUtc)
        {
            Id = id;
            CaseNumber = caseNumber;
            AssetId = assetId;
            Status = status;
            IssueType = issueType;
            OtherIssueDescription = otherIssueDescription;
            Description = description;
            OriginStoreId = originStoreId;
            CreatedByUserId = createdByUserId;
            CreatedByUserName = createdByUserName;
            CreatedAtUtc = createdAtUtc;
        }

        public static ServiceCase Create(
            string caseNumber,
            Guid assetId,
            ServiceCaseStatus initialStatus,
            ServiceIssueType issueType,
            string? otherIssueDescription,
            string description,
            Guid? originStoreId,
            Guid createdByUserId,
            string createdByUserName,
            DateTime createdAtUtc)
        {
            ValidateCaseNumber(caseNumber);
            ValidateIdentifier(assetId, nameof(assetId));
            ValidateInitialStatus(initialStatus);
            ValidateIssueType(issueType);
            ValidateDescription(description);
            ValidateIdentifier(createdByUserId, nameof(createdByUserId));
            ValidateCreatedByUserName(createdByUserName);
            ValidateOriginStore(initialStatus, originStoreId);
            ValidateUtcDateTime(createdAtUtc, nameof(createdAtUtc));
            var normalizedOtherIssueDescription = ValidateAndNormalizeOtherIssueDescription(issueType, otherIssueDescription);

            return new ServiceCase(
                id: Guid.NewGuid(),
                caseNumber: caseNumber.Trim(),
                assetId: assetId,
                status: initialStatus,
                issueType: issueType,
                otherIssueDescription: normalizedOtherIssueDescription,
                description: description.Trim(),
                originStoreId: originStoreId,
                createdByUserId: createdByUserId,
                createdByUserName: createdByUserName.Trim(),
                createdAtUtc: createdAtUtc
            );
        }

        public void ReceiveAtHeadquarters()
        {
            if (Status != ServiceCaseStatus.AwaitingCentralReceipt)
            {
                throw new InvalidOperationException("Only a service case awaiting central receipt can be received at headquarters.");
            }

        Status = ServiceCaseStatus.UnderCentralInspection;
        }
        public void RequireExternalService(Guid vendorId)
        {
            if (Status != ServiceCaseStatus.UnderCentralInspection)
            {
                throw new InvalidOperationException("External service can only be requested while the service case is under central inspection.");
            }

            if (vendorId == Guid.Empty)
            {
                throw new ArgumentException("The vendor identifier cannot be empty.", nameof(vendorId));
            }

            VendorId = vendorId;
            Status = ServiceCaseStatus.AwaitingExternalServiceDispatch;
        }
        public void DispatchToExternalService()
        {
            if (Status != ServiceCaseStatus.AwaitingExternalServiceDispatch)
            {
                throw new InvalidOperationException("Only a service case awaiting external service dispatch can be sent to external service.");
            }

            if (!VendorId.HasValue)
            {
                throw new InvalidOperationException("A vendor must be assigned before the service case can be sent to external service.");
            }

            if (VendorId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("The assigned vendor identifier cannot be empty.");
            }

            Status = ServiceCaseStatus.SentToExternalService;
        }

        public void HandOverToExternalService()
        {
            if (Status != ServiceCaseStatus.AwaitingExternalServiceDispatch)
            {
                throw new InvalidOperationException("Only a service case awaiting external service dispatch can be handed over to external service.");
            }

            if (!VendorId.HasValue || VendorId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("A vendor must be assigned before the asset can be handed over to external service.");
            }

            Status = ServiceCaseStatus.InExternalService;
        }
        public void ConfirmReceivedByExternalService()
        {
            if (Status != ServiceCaseStatus.SentToExternalService)
            {
                throw new InvalidOperationException("Only a service case sent to external service can be marked as received by the external service.");
            }

            if (!VendorId.HasValue || VendorId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("A valid vendor must be assigned before external service receipt can be confirmed.");
            }

            Status = ServiceCaseStatus.InExternalService;
        }

        public void ReceiveFromExternalService()
        {
            if (Status != ServiceCaseStatus.InExternalService)
            {
                throw new InvalidOperationException("Only a service case currently in external service can be received from external service.");
            }

            if (!VendorId.HasValue || VendorId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("A valid vendor must be assigned before the asset can be received from external service.");
            }

            Status = ServiceCaseStatus.AwaitingReturn;
        }
        public void ReturnFromExternalService()
        {
            if (Status != ServiceCaseStatus.InExternalService)
            {
                throw new InvalidOperationException("Only a service case currently in external service can be returned from external service.");
            }

            Status = ServiceCaseStatus.AwaitingReturn;
        }
        public void DispatchToOriginStore()
        {
            if (Status != ServiceCaseStatus.AwaitingReturn)
            {
                throw new InvalidOperationException("Only a service case awaiting return can be dispatched to the origin store.");
            }

            if (!OriginStoreId.HasValue || OriginStoreId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("The service case does not have a valid origin store.");
            }

            Status = ServiceCaseStatus.ReturningToStore;
        }
        public void ReceiveAtOriginStore()
        {
            if (Status != ServiceCaseStatus.ReturningToStore)
            {
                throw new InvalidOperationException("Only a service case returning to its origin store can be received at the origin store.");
            }

            if (!OriginStoreId.HasValue || OriginStoreId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("The service case does not have a valid origin store.");
            }

            Status = ServiceCaseStatus.Resolved;
        }

        public void CompleteRepairAtHeadquarters()
        {
            if (Status != ServiceCaseStatus.UnderCentralInspection)
            {
                throw new InvalidOperationException("Only a service case under central inspection can have its repair completed at headquarters.");
            }

            if (OriginStoreId == Guid.Empty)
            {
                throw new InvalidOperationException("The origin store identifier cannot be empty.");
            }

            Status = OriginStoreId.HasValue ? ServiceCaseStatus.AwaitingReturn : ServiceCaseStatus.Resolved;
        }
        public void ResolveAfterExternalServiceReturn()
        {
            if (Status != ServiceCaseStatus.AwaitingReturn)
            {
                throw new InvalidOperationException("Only a service case awaiting return can be resolved after external service.");
            }

            if (OriginStoreId.HasValue)
            {
                throw new InvalidOperationException("A service case opened from a store must be returned to its origin store before it can be resolved.");
            }

            if (!VendorId.HasValue ||
                VendorId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("A valid vendor must be assigned before the service case can be resolved after external service.");
            }

            Status = ServiceCaseStatus.Resolved;
        }

        /// <summary>
        /// Resolves the service case following disposal of its asset.
        /// </summary>
        /// <remarks>
        /// Disposal is permitted while the service case is under central
        /// inspection, awaiting external service dispatch, currently
        /// in external service, or awaiting return.
        ///
        /// The asset does not have to be returned to its origin store
        /// or headquarters before the service case can be resolved
        /// through disposal.
        ///
        /// This method changes only the service case status.
        /// The application handler must separately validate the related
        /// asset state, dispose of the asset, record the disposal event
        /// and asset history entries, and persist all changes together.
        ///
        /// Authorization must be enforced by the application layer.
        /// Only active application administrators may perform disposal.
        ///
        /// The service case is not closed automatically.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The current service case status does not allow asset disposal,
        /// or an external service case does not have a valid vendor.
        /// </exception>
        public void ResolveByAssetDisposal()
        {
            if (Status is not (
                ServiceCaseStatus.UnderCentralInspection or
                ServiceCaseStatus.AwaitingExternalServiceDispatch or
                ServiceCaseStatus.InExternalService or
                ServiceCaseStatus.AwaitingReturn))
            {
                throw new InvalidOperationException("Only a service case under central inspection, awaiting external service dispatch, in external service, or awaiting return can be resolved by asset disposal.");
            }

            if (Status is (
                ServiceCaseStatus.AwaitingExternalServiceDispatch or
                ServiceCaseStatus.InExternalService) &&
                (!VendorId.HasValue || VendorId.Value == Guid.Empty))
            {
                throw new InvalidOperationException("A valid vendor must be assigned before the service case can be resolved by asset disposal from an external service stage.");
            }

            Status = ServiceCaseStatus.Resolved;
        }

        public void Close(Guid closedByUserId, DateTime closedAtUtc)
        {
            if (Status != ServiceCaseStatus.Resolved)
            {
                throw new InvalidOperationException("Only a resolved service case can be closed.");
            }

            if (closedByUserId == Guid.Empty)
            {
                throw new ArgumentException("The closing user identifier cannot be empty.", nameof(closedByUserId));
            }

            if (closedAtUtc == default)
            {
                throw new ArgumentException("The closing date cannot be empty.", nameof(closedAtUtc));
            }

            if (closedAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("The closing date must be in UTC.", nameof(closedAtUtc));
            }

            if (closedAtUtc < CreatedAtUtc)
            {
                throw new ArgumentException("The closing date cannot be earlier than the creation date.", nameof(closedAtUtc));
            }

            ClosedByUserId = closedByUserId;
            ClosedAtUtc = closedAtUtc;
            Status = ServiceCaseStatus.Closed;
        }

        private static void ValidateCaseNumber(string caseNumber)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(caseNumber);

            if (caseNumber.Length > 50)
            {
                throw new ArgumentException("Case number cannot exceed 50 characters.", nameof(caseNumber));
            }
        }
        private static void ValidateIdentifier(Guid id, string paramName)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException($"{paramName} cannot be an empty GUID.", paramName);
            }
        }
        private static void ValidateInitialStatus(ServiceCaseStatus status)
        {
            if(!Enum.IsDefined(status) || status == ServiceCaseStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid initial status." );
            }
            if (status is not ServiceCaseStatus.AwaitingCentralReceipt and not ServiceCaseStatus.UnderCentralInspection)
            {
                throw new ArgumentException("A service case must start by awaiting central receipt or under central inspection.", nameof(status));
            }
        }
        private static void ValidateIssueType(ServiceIssueType issueType)
        {
            if (!Enum.IsDefined(issueType) || issueType == ServiceIssueType.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(issueType), issueType, "Invalid issue type." );
            }
        }
        private static void ValidateDescription(string description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            if (description.Length > 2000)
            {
                throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(description));
            }
        }
        private static string? ValidateAndNormalizeOtherIssueDescription(ServiceIssueType issueType, string? otherIssueDescription)
        {
            if (issueType == ServiceIssueType.Other)
            {
                if (string.IsNullOrWhiteSpace(otherIssueDescription))
                {
                    throw new ArgumentException("Other issue description is required when the selected issue type is Other.", nameof(otherIssueDescription));
                }

                if (otherIssueDescription.Length > 250)
                {
                    throw new ArgumentException("Other issue description cannot exceed 250 characters.", nameof(otherIssueDescription));
                }

                return otherIssueDescription.Trim();
            }

            if (!string.IsNullOrWhiteSpace(otherIssueDescription))
            {
                throw new ArgumentException("Other issue description can only be specified when the selected issue type is Other.", nameof(otherIssueDescription));
            }

            return null;
        }
        private static void ValidateCreatedByUserName(string createdByUserName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserName);

            if (createdByUserName.Length > 250)
            {
                throw new ArgumentException(
                    "The creator display name cannot exceed 250 characters.",
                    nameof(createdByUserName));
            }
        }

        private static void ValidateUtcDateTime(DateTime value, string parameterName)
        {
            if (value == default)
            {
                throw new ArgumentException("The date cannot be empty.", parameterName);
            }

            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("The date must be in UTC.", parameterName);
            }
        }

        private static void ValidateOriginStore(ServiceCaseStatus initialStatus, Guid? originStoreId)
        {
            if (originStoreId == Guid.Empty)
            {
                throw new ArgumentException("The origin store identifier cannot be empty.", nameof(originStoreId));
            }

            if (initialStatus == ServiceCaseStatus.AwaitingCentralReceipt && !originStoreId.HasValue)
            {
                throw new ArgumentException("An origin store is required when the service case is opened from a store.", nameof(originStoreId));
            }

            if (initialStatus == ServiceCaseStatus.UnderCentralInspection && originStoreId.HasValue)
            {
                throw new ArgumentException("An origin store cannot be specified when the service case is opened at headquarters.", nameof(originStoreId));
            }
        }
    }
}