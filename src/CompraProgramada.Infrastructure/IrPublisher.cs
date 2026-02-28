using System.Text.Json;
using System.Threading.Tasks;
using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Infrastructure
{
    public class IrPublisher
    {
        private readonly KafkaProducer _producer;

        public IrPublisher(KafkaProducer producer)
        {
            _producer = producer;
        }

        public Task PublishDedoDuro(IrDedoDuroEvent evt)
        {
            var json = JsonSerializer.Serialize(evt);
            return _producer.ProduceAsync(evt.ClienteId.ToString(), json);
        }

        public Task PublishVenda(IrVendaEvent evt)
        {
            var json = JsonSerializer.Serialize(evt);
            return _producer.ProduceAsync(evt.ClienteId.ToString(), json);
        }
    }
}
