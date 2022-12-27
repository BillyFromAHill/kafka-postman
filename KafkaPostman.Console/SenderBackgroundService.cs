using Microsoft.Extensions.Hosting;

namespace KafkaPostman;

public class SenderBackgroundService : BackgroundService
{
    private readonly IJsonPostman _jsonPostman;

    public SenderBackgroundService(IJsonPostman jsonPostman)
    {
        _jsonPostman = jsonPostman;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var newMessage = Console.ReadLine();

            await _jsonPostman.SendAsync(newMessage!, stoppingToken);
        }
    }
}