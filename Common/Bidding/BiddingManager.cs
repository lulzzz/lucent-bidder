using System.Collections.Generic;
using System.Threading.Tasks;
using Lucent.Common.Entities;
using Lucent.Common.Events;
using Lucent.Common.Messaging;
using Lucent.Common.Serialization;
using Lucent.Common.Storage;
using Microsoft.Extensions.Logging;

namespace Lucent.Common.Bidding
{
    /// <summary>
    /// Class to manage bidding infrastructure, events and common objects
    /// </summary>
    public class BiddingManager : IBiddingManager
    {
        ILogger<BiddingManager> _log;
        ISerializationContext _serializationContext;
        IStorageManager _storageManager;
        IMessageFactory _messageFactory;
        IBidFactory _bidFactory;
        IMessageSubscriber<EntityEventMessage> _entityEvents;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serializationContext"></param>
        /// <param name="storageManager"></param>
        /// <param name="messageFactory"></param>
        /// <param name="bidFactory"></param>
        public BiddingManager(ILogger<BiddingManager> logger, ISerializationContext serializationContext, IStorageManager storageManager, IMessageFactory messageFactory, IBidFactory bidFactory)
        {
            _log = logger;
            _serializationContext = serializationContext;
            _storageManager = storageManager;
            _messageFactory = messageFactory;
            _bidFactory = bidFactory;
            _entityEvents = _messageFactory.CreateSubscriber<EntityEventMessage>("bidding", 0, "campaign");
            _entityEvents.OnReceive = HandleMessage;
        }

        /// <summary>
        /// Entity change handler
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        async Task HandleMessage(EntityEventMessage message)
        {
            _log.LogInformation("new message : {0}", message.MessageId);

            if (message.Body != null)
            {
                var id = message.Body.EntityId;
                switch (message.Body.EntityType)
                {
                    case EntityType.Campaign:
                        switch (message.Body.EventType)
                        {
                            case EventType.EntityAdd:
                            case EventType.EntityUpdate:
                                var entity = await _storageManager.GetBasicRepository<Campaign>().Get(id);
                                Bidders.RemoveAll(b => b.Campaign.Id.Equals(id));
                                if (entity != null)
                                {
                                    _log.LogInformation("Added bidder for campaign : {0}", id);
                                    Bidders.Add(_bidFactory.CreateBidder(entity));
                                }
                                break;
                            case EventType.EntityDelete:
                                _log.LogInformation("Removing bidder for campaign : {0}", id);
                                Bidders.RemoveAll(b => b.Campaign.Id.Equals(id));
                                break;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Get the active bidders
        /// </summary>
        /// <returns></returns>
        public List<ICampaignBidder> Bidders { get; private set; } = new List<ICampaignBidder>();
    }
}