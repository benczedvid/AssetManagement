using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Tests.Fakes
{
    public sealed class FakeServiceDeskRequestClosureOutboxRepository : IServiceDeskRequestClosureOutboxRepository
    {
        private readonly List<ServiceDeskRequestClosureOutbox> _outboxes = [];
        public IReadOnlyList<ServiceDeskRequestClosureOutbox> Outboxes => _outboxes.AsReadOnly();
        public int AddCallCount { get; private set; }
        public CancellationToken LastAddCancellationToken { get; private set; }

        public Task AddAsync(ServiceDeskRequestClosureOutbox outbox, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(outbox);

            AddCallCount++;
            LastAddCancellationToken = cancellationToken;
            _outboxes.Add(outbox);

            return Task.CompletedTask;
        }

        public Task<ServiceDeskRequestClosureOutbox?> GetByIdAsync(Guid outboxId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_outboxes.SingleOrDefault(outbox => outbox.Id == outboxId));
        }

        public Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Outbox processing is not supported by this fake.");
        }

        public Task<bool> TryBeginProcessingAsync(Guid outboxId, Guid operationId, DateTime startedAtUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Outbox processing is not supported by this fake.");
        }

        public Task<bool> TryCompleteAsync(Guid outboxId, Guid operationId, DateTime completedAtUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Outbox processing is not supported by this fake.");
        }

        public Task<bool> TryRequireReconciliationAsync(Guid outboxId, Guid operationId, string reason, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Outbox processing is not supported by this fake.");
        }

        public Task<IReadOnlyList<Guid>> GetStaleProcessingIdsAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Outbox processing is not supported by this fake.");
        }

        public Task<bool> TryMarkStaleProcessingForReconciliationAsync(Guid outboxId, DateTime olderThanUtc, string reason, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Outbox processing is not supported by this fake.");
        }

        public Task<bool> TryFailReconciliationAsync(Guid outboxId, Guid operationId, Guid verifiedByUserId, string verifiedByUserName, DateTime verifiedAtUtc, string verificationComment, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Outbox processing is not supported by this fake.");
        }

        public Task<bool> TryCompleteReconciliationAsync(Guid outboxId, Guid operationId, Guid verifiedByUserId, string verifiedByUserName, DateTime verifiedAtUtc, string verificationComment, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Outbox processing is not supported by this fake.");
        }
    }
}
