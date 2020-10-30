using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrecisionReporters.Platform.Data.Handlers
{
    public class TransactionInfo : ITransactionInfo
    {
        private readonly Dictionary<Guid, TransactionRecord> _records = new Dictionary<Guid, TransactionRecord>();
        
        public TransactionRecord this[Guid transactionId]
        {
            get
            {
                if (_records.TryGetValue(transactionId, out var result))
                {
                    return result;
                }
                else
                {
                    return _records[transactionId] = new TransactionRecord(transactionId);
                }
            }
        }        

        public TransactionRecord CreateSnapshotRecord(ChangeTracker changeTracker)
        {
            var record = new TransactionRecord(null);
            UpdateRecord(changeTracker, record);
            return record;
        }

        public void UpdateTransactionInfo(Guid transactionId, ChangeTracker changeTracker)
        {
            var record = this[transactionId];
            UpdateRecord(changeTracker, record);
        }

        private static void UpdateRecord(ChangeTracker changeTracker, TransactionRecord record)
        {
            var currentList = record.AddedEntities.ToList();
            var addedEntities = from entity in changeTracker.Entries()
                                where entity.State == EntityState.Added && !currentList.Contains(entity.Entity)
                                select entity.Entity;
            record.AddedEntities.AddRange(addedEntities);
        }
    }
}
