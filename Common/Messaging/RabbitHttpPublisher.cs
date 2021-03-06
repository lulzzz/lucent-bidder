using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Lucent.Common.Serialization;
using System.Threading;

namespace Lucent.Common.Messaging
{
    internal class RabbitHttpPublisher : IMessagePublisher
    {
        static readonly HttpClient _client;

        static RabbitHttpPublisher()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            _client = new HttpClient();
        }

        Uri _publishEndpoint;
        RabbitCluster _cluster;
        IMessageFactory _factory;
        readonly ILogger _log;
        ISerializationContext _serializationContext;

        public RabbitHttpPublisher(IMessageFactory factory, ILogger log, RabbitCluster clusterInfo, ISerializationContext serializationContext, string topic)
        {
            _log = log;
            _serializationContext = serializationContext;
            _factory = factory;
            _publishEndpoint = new UriBuilder
            {
                Scheme = "https",
                Host = clusterInfo.Host,
                Path = "/api/exchanges/{0}/{1}/publish".FormatWith(WebUtility.UrlEncode(clusterInfo.VHost), topic)
            }.Uri;
            _cluster = clusterInfo;

            Topic = topic;
        }

        public string Topic { get; set; }

        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<bool> TryBroadcast(IMessage message)
        {
            using (var ctx = MessageCounters.LatencyHistogram.CreateContext(Topic, "broadcast"))
                try
                {
                    var res = true;
                    foreach (var cluster in _factory.GetClusters())
                        res &= await _factory.CreatePublisher(cluster, Topic).TryPublish(message);
                    return res;
                }
                catch (Exception e)
                {
                    _log.LogWarning(e, "Failed to broadcast message on {0}", Topic);
                    MessageCounters.ErrorCounter.WithLabels(Topic, e.GetType().Name).Inc();
                }

            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> TryPublish(IMessage message)
        {
            using (var ctx = MessageCounters.LatencyHistogram.CreateContext(Topic, "http_publish"))
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        if (message.ContentType != null)
                            message.Headers.Add("Content-Type", message.ContentType);

                        var httpMessage = new RabbitHttpMessage
                        {
                            VHost = _cluster.VHost,
                            Properties = new RabbitHttpMessageProperties
                            {
                                ContentType = message.ContentType,
                                Headers = new RabbitHttpHeaders
                                {
                                    ContentType = message.ContentType,
                                },
                            },
                            RoutingKey = message.Route,
                            Payload = Convert.ToBase64String(await message.ToBytes()),
                            Headers = new RabbitHttpHeaders
                            {
                                ContentType = message.ContentType,
                            },
                        };

                        await _serializationContext.WriteTo(httpMessage, ms, true, SerializationFormat.JSON);

                        var msg = Encoding.UTF8.GetString(ms.ToArray());

                        using (var req = new HttpRequestMessage(HttpMethod.Post, _publishEndpoint.ToString()))
                        {
                            var cts = new CancellationTokenSource(2000);
                            req.Content = new StringContent(msg, Encoding.UTF8, "application/json");
                            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("{0}:{1}".FormatWith(_cluster.User, _cluster.Credentials))));
                            var response = await _client.SendAsync(req, cts.Token);

                            return response.IsSuccessStatusCode;
                        }
                    }
                }
                catch (Exception e)
                {
                    _log.LogWarning(e, "Failed to publish message on {0}", Topic);
                    MessageCounters.ErrorCounter.WithLabels(Topic, e.GetType().Name).Inc();
                }

            return false;
        }
    }

    internal class RabbitHttpMessage
    {
        [SerializationProperty(1, "vhost")]
        public string VHost { get; set; }

        [SerializationProperty(2, "name")]
        public string Name { get; set; }

        [SerializationProperty(3, "properties")]
        public RabbitHttpMessageProperties Properties { get; set; }

        [SerializationProperty(4, "routing_key")]
        public string RoutingKey { get; set; }

        [SerializationProperty(5, "payload")]
        public string Payload { get; set; }

        [SerializationProperty(6, "payload_encoding")]
        public string PayloadEncoding { get; set; } = "base64";

        [SerializationProperty(7, "headers")]
        public RabbitHttpHeaders Headers { get; set; }
    }

    internal class RabbitHttpMessageProperties
    {
        [SerializationProperty(1, "deliver_mode")]
        public int DeliveryMode { get; set; } = 2;

        [SerializationProperty(2, "content_type")]
        public string ContentType { get; set; }

        [SerializationProperty(3, "headers")]
        public RabbitHttpHeaders Headers { get; set; }
    }

    internal class RabbitHttpHeaders
    {
        [SerializationProperty(1, "Content-Type")]
        public string ContentType { get; set; }
    }
}