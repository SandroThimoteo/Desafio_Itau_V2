using System;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace CompraProgramada.Infrastructure
{
    public class KafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;

        public KafkaProducer(string bootstrapServers, string topic)
        {
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<string, string>(config).Build();
            _topic = topic;
        }

        public async Task ProduceAsync(string key, string message)
        {
            var msg = new Message<string, string> { Key = key, Value = message };
            var delivery = await _producer.ProduceAsync(_topic, msg);
            // log or handle delivery status
        }
    }
}
