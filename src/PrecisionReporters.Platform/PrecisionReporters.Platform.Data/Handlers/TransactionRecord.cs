using System;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.Data.Handlers
{
    /// <summary>
    /// Maintains a record of what has been altered as part of a transaction
    /// </summary>
    public class TransactionRecord
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="transactionId">
        /// The ID of the transaction this record will cover, or <see langword="null"/> if this
        /// record is a snapshot outside of a transaction
        /// </param>
        public TransactionRecord(Guid? transactionId)
        {
            TransactionId = transactionId;
        }
        /// <summary>
        /// The ID of the transaction this record will cover, or <see langword="null"/> if this
        /// record is a snapshot outside of a transaction
        /// </summary>
        public Guid? TransactionId { get; }
        /// <summary>
        /// The new entities added within the given transaction
        /// </summary>
        public List<object> AddedEntities { get; } = new List<object>();
    }
}
