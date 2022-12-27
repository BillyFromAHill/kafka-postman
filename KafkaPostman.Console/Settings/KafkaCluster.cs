namespace KafkaPostman.Settings;

public class KafkaCluster
{
    public string? BootstrapServers { get; set; }

    public string? SaslUserName { get; set; }

    public string? SaslPassword { get; set; }
}