using KafkaFlow;
using KafkaPostman.Kafka;

namespace KafkaPostman.KafkaFlow;

public class KafkaFlowSender : ISender
{
    private readonly IMessageProducer<KafkaFlowSender> _producer;

    public KafkaFlowSender(IMessageProducer<KafkaFlowSender> producer)
    {
        _producer = producer;
    }

    public async Task SendAsync(byte[] data, CancellationToken ct)
    {
        // TODO: Implement key selector.
        await _producer.ProduceAsync(null, data);
    }
}