using System.Collections.Concurrent;
using System.Threading.Tasks;
using Lucent.Common.Messaging;

namespace Lucent.Common.Budget
{
    /// <summary>
    /// This is terrible
    /// </summary>
    public class SimpleBudgetManager : IBudgetManager
    {
        ConcurrentDictionary<string, decimal> _budgets = new ConcurrentDictionary<string, decimal>();
        IMessageSubscriber<BudgetEventMessage> _budgetSubscriber;
        IBudgetClient _budgetClient;

        /// <summary>
        /// Useless
        /// </summary>
        public SimpleBudgetManager(IMessageFactory messageFactory, IBudgetClient budgetClient)
        {
            _budgetSubscriber = messageFactory.CreateSubscriber<BudgetEventMessage>("budget", 0, messageFactory.WildcardFilter);
            _budgetSubscriber.OnReceive = HandleBudgetRequests;
            _budgetClient = budgetClient;
        }

        /// <summary>
        /// Handle events
        /// </summary>
        /// <param name="budgetEvent"></param>
        /// <returns></returns>
        async Task HandleBudgetRequests(BudgetEventMessage budgetEvent)
        {
            _budgets.AddOrUpdate(budgetEvent.Body.EntityId, budgetEvent.Body.Amount, (eid, o) => o += budgetEvent.Body.Amount);
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task GetAdditional(string id) => await GetAdditional(1m, id);

        /// <inheritdoc/>
        public async Task GetAdditional(decimal amount, string id) => await _budgetClient.RequestBudget(id, amount);

        /// <inheritdoc/>
        public async Task<decimal> GetRemaining(string id)
        {
            return await Task.FromResult(_budgets.GetOrAdd(id, 0m));
        }

        /// <inheritdoc/>
        public bool IsExhausted(string id) => _budgets.GetOrAdd(id, 0m) <= 0m;

        /// <inheritdoc/>
        public async Task<bool> TrySpend(decimal amount, string id)
        {
            return await Task.FromResult(_budgets.AddOrUpdate(id, -amount, (i, o) => o - amount) >= 0m);
        }
    }
}