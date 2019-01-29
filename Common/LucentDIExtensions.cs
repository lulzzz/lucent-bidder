using Lucent.Common.Messaging;
using Lucent.Common.Serialization;
using Lucent.Common.Storage;
using Lucent.Common.Bidding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lucent.Common.Exchanges;
using Lucent.Common.Entities;
using Lucent.Common.Media;
using Lucent.Common.Entities.Repositories;
using System;
using Lucent.Common.Budget;
using Lucent.Common.Client;
using Microsoft.Extensions.Caching.Memory;
using Lucent.Common.Caching;

namespace Lucent.Common
{
    /// <summary>
    /// Dependency Injection extensions
    /// </summary>
    public static partial class LucentDIExtensions
    {
        /// <summary>
        /// Sets up the bidder
        /// </summary>
        /// <param name="services">The current services collection</param>
        /// <param name="configuration">The current configuration</param>
        /// <param name="localOnly">Indicate if this is a setup for local resources only</param>
        /// <param name="includePortal">Flag for portal servicees</param>
        /// <param name="includeBidder">Flag for bidder services</param>
        /// <param name="includeOrchestration">Flag for orchestartion services</param>
        /// <param name="includeScoring">Flag for scoring services</param>
        /// <returns>A modified set of services</returns>
        public static IServiceCollection AddLucentServices(this IServiceCollection services, IConfiguration configuration, bool localOnly = false, bool includePortal = false, bool includeBidder = false, bool includeOrchestration = false, bool includeScoring = false)
        {
            // Enable routing
            services.AddRouting();

            // Add serialization
            services.AddSingleton<ISerializationContext, LucentSerializationContext>();
            services.AddTransient<IClientManager, DefaultClientManager>();

            // Setup storage, messaging options for local vs distributed cluster
            if (localOnly)
            {
                services.Configure<MemoryCacheOptions>((opts) =>
                {
                    opts.ExpirationScanFrequency = TimeSpan.FromSeconds(30);
                    opts.SizeLimit = 1024 * 1024 * 256L;
                }).AddSingleton<IMemoryCache, MemoryCache>().AddSingleton<IBidderCache, MemoryBidderCache>();
                services.AddSingleton<IStorageManager, InMemoryStorage>();
                services.AddSingleton<IMessageFactory, InMemoryMessageFactory>();
                services.Configure<BudgetLedgerConfig>(configuration.GetSection("ledger"))
                    .AddSingleton<IBudgetLedgerManager, InMemoryBudgetLedgerManager>();
            }
            else
            {
                // TODO: Update this
                services.Configure<MemoryCacheOptions>((opts) =>
                {
                    opts.ExpirationScanFrequency = TimeSpan.FromSeconds(30);
                    opts.SizeLimit = 1024 * 1024 * 256L;
                }).AddSingleton<IMemoryCache, MemoryCache>().AddSingleton<IBidderCache, MemoryBidderCache>();

                // Setup storage
                services.Configure<CassandraConfiguration>(configuration.GetSection("cassandra"))
                    .AddSingleton<IStorageManager, CassandraStorageManager>();

                services.Configure<RabbitConfiguration>(configuration.GetSection("rabbit"))
                    .AddSingleton<IMessageFactory, RabbitFactory>();

                var storageManager = services.BuildServiceProvider().GetRequiredService<IStorageManager>();

                // Register custom repositories
                storageManager.RegisterRepository<LedgerEntryRepository, LedgerEntry>();
                storageManager.RegisterRepository<ExchangeEntityRespositry, Exchange>();
            }

            if (includePortal)
            {
                services.Configure<MediaConfig>(configuration.GetSection("media"))
                    .AddSingleton<IMediaScanner, MediaScanner>();
            }

            if (includeBidder)
            {
                // Setup bidder
                services.Configure<BudgetConfig>(configuration.GetSection("budget"))
                    .AddScoped<IBudgetClient, SimpleBudgetClient>()
                    .AddScoped<IBudgetManager, SimpleBudgetManager>()
                    .AddScoped<IBiddingManager, BiddingManager>()
                    .AddSingleton<IBidFactory, BidFactory>()
                    .AddSingleton<IExchangeRegistry, ExchangeRegistry>();
            }

            if (includeOrchestration)
            {

            }

            if (includeScoring)
            {

            }

            return services;
        }
    }
}