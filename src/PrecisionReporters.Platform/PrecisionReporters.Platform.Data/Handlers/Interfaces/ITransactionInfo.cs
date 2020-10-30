using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;

namespace PrecisionReporters.Platform.Data.Handlers.Interfaces
{
    public interface ITransactionInfo
    {
        TransactionRecord CreateSnapshotRecord(ChangeTracker changeTracker);
        void UpdateTransactionInfo(Guid transactionId, ChangeTracker changeTracker);
    }
}
