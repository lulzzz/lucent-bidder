using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lucent.Common;
using Lucent.Common.Entities;
using Lucent.Common.Entities.Events;
using Lucent.Common.Events;
using Lucent.Common.Messaging;
using Lucent.Common.Serialization;
using Lucent.Common.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lucent.Common.Middleware
{
    /// <summary>
    /// Handle BidderFilter API management
    /// </summary>
    public class BidderFilterOrchestrator
    {
        readonly IStorageManager _storageManager;
        readonly ISerializationContext _serializationContext;
        readonly ILogger<BidderFilterOrchestrator> _logger;
        readonly IBasicStorageRepository<BidderFilter> _bidderFilterRepository;
        readonly IMessageFactory _messageFactory;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="storageManager"></param>
        /// <param name="messageFactory"></param>
        /// <param name="serializationContext"></param>
        /// <param name="logger"></param>
        public BidderFilterOrchestrator(RequestDelegate next, IStorageManager storageManager, IMessageFactory messageFactory, ISerializationContext serializationContext, ILogger<BidderFilterOrchestrator> logger)
        {
            _storageManager = storageManager;
            _bidderFilterRepository = storageManager.GetBasicRepository<BidderFilter>();
            _serializationContext = serializationContext;
            _messageFactory = messageFactory;
            _logger = logger;

            _messageFactory.CreateSubscriber<LucentMessage<BidderFilter>>("entities", 0, "bidderFilter").OnReceive += UpdateBidderFilters;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bidderFilterEvent"></param>
        /// <returns></returns>
        async Task UpdateBidderFilters(LucentMessage<BidderFilter> bidderFilterEvent)
        {
            if (bidderFilterEvent.Body != null)
            {
                var evt = new EntityEvent
                {
                    EntityType = EntityType.BidFilter,
                    EntityId = bidderFilterEvent.Body.Id,
                };

                // This is awful, don't do this for real
                if (await _bidderFilterRepository.TryUpdate(bidderFilterEvent.Body))
                    evt.EventType = EventType.EntityUpdate;
                else if (await _bidderFilterRepository.TryInsert(bidderFilterEvent.Body))
                    evt.EventType = EventType.EntityAdd;
                else if (await _bidderFilterRepository.TryRemove(bidderFilterEvent.Body))
                    evt.EventType = EventType.EntityDelete;

                // Notify
                if (evt.EventType != EventType.Unknown)
                {
                    var msg = _messageFactory.CreateMessage<EntityEventMessage>();
                    msg.Body = evt;
                    msg.Route = "bidderFilter";
                    using (var ms = new MemoryStream())
                    {
                        await _serializationContext.WriteTo(evt, ms, true, SerializationFormat.JSON);
                    }

                    await _messageFactory.CreatePublisher("bidding").TryPublish(msg);
                }
            }
        }


        /// <summary>
        /// Handle the call asynchronously
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            var c = await _serializationContext.ReadAs<BidderFilter>(httpContext);
            if (c != null)
            {
                // Validate
                var evt = new EntityEvent
                {
                    EntityType = EntityType.BidFilter,
                    EntityId = c.Id,
                };

                switch (httpContext.Request.Method.ToLowerInvariant())
                {
                    case "post":
                        if (await _bidderFilterRepository.TryInsert(c))
                        {
                            httpContext.Response.StatusCode = 201;
                            evt.EventType = EventType.EntityAdd;
                            evt.EntityId = c.Id;
                            await _serializationContext.WriteTo(httpContext, c);
                        }
                        else
                            httpContext.Response.StatusCode = 409;
                        break;
                    case "put":
                    case "patch":
                        if (await _bidderFilterRepository.TryUpdate(c))
                        {
                            httpContext.Response.StatusCode = 202;
                            evt.EventType = EventType.EntityUpdate;
                            await _serializationContext.WriteTo(httpContext, c);
                        }
                        else
                            httpContext.Response.StatusCode = 409;
                        break;
                    case "delete":
                        if (await _bidderFilterRepository.TryRemove(c))
                        {
                            evt.EventType = EventType.EntityDelete;
                            httpContext.Response.StatusCode = 204;
                        }
                        else
                            httpContext.Response.StatusCode = 404;
                        break;
                }

                // Notify
                if (evt.EventType != EventType.Unknown)
                {
                    var msg = _messageFactory.CreateMessage<EntityEventMessage>();
                    msg.Body = evt;
                    msg.Route = "bidderFilter";
                    await _messageFactory.CreatePublisher("bidding").TryPublish(msg);

                    var sync = _messageFactory.CreateMessage<LucentMessage<BidderFilter>>();
                    sync.Body = c;
                    sync.Route = "bidderFilter";
                    await _messageFactory.CreatePublisher("entities").TryBroadcast(msg);
                }
            }
        }
    }
}