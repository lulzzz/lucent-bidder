using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lucent.Common.Entities;
using Lucent.Common.OpenRTB;

namespace Lucent.Common.Budget
{
    /// <summary>
    /// Stores bid history
    /// </summary>
    public interface IBidLedger
    {
        /// <summary>
        /// Tries to record an entry with the source and amount
        /// </summary>
        Task<bool> TryRecordEntry(string ledgerId, BidEntry source);

        /// <summary>
        /// Get the summary over the given start and end dates for the entity, divided into the specified number of segments
        /// </summary>
        /// <param name="entityId">The entity</param>
        /// <param name="start">The start time (inclusive)</param>
        /// <param name="end">The end time (exclusive)</param>
        /// <param name="numSegments">The optional number of segments (Default = 1)</param>
        /// <returns></returns>
        Task<ICollection<LedgerSummary>> TryGetSummary(string entityId, DateTime start, DateTime end, int? numSegments);
    }

    /// <summary>
    /// Summary of a given timespan
    /// </summary>
    public class LedgerSummary
    {
        /// <summary>
        /// The start for this block
        /// </summary>
        /// <value></value>
        public DateTime Start { get; set; }

        /// <summary>
        /// The end of the block
        /// </summary>
        /// <value></value>
        public DateTime End { get; set; }

        /// <summary>
        /// The total spent during this block
        /// </summary>
        /// <value></value>
        public double Amount { get; set; }

        /// <summary>
        /// The number of bids won
        /// </summary>
        /// <value></value>
        public int Bids { get; set; }
    }
}