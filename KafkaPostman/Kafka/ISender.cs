namespace KafkaPostman.Kafka;

public interface ISender
{
    public Task SendAsync(byte[] data, CancellationToken ct);
}