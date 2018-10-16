using Lucent.Common.Entities;

namespace Lucent.Common.Bidding
{
    /// <summary>
    /// Bid factory
    /// </summary>
    public interface IBidFactory
    {
        /// <summary>
        /// Create a bidder for the given campaign
        /// </summary>
        /// <param name="campaign">The campaign</param>
        /// <returns>A bidder for the campaign</returns>
        ICampaignBidder CreateBidder(Campaign campaign);

        /// <summary>
        /// Gets a new budget ledger manager
        /// </summary>
        /// <returns><A new budget ledger manager/returns>
        IBudgetLedgerManager CreateBudgetManager();
    }
}