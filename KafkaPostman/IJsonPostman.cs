namespace KafkaPostman;

public interface IJsonPostman
{
    public Task SendAsync(string json, CancellationToken ct);
}