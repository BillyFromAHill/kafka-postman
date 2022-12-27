using KafkaFlow;
using KafkaPostman.Kafka;
using KafkaPostman.KafkaFlow;
using KafkaPostman.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaPostman;

static class Program
{
    public static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder =>
            {
                builder
                    .AddCommandLine(args)
                    .AddEnvironmentVariables()
                    .AddJsonFile("appsettings.json");
            })
            .ConfigureServices((context, services) =>
            {
                var clusterSettings =
                    context.Configuration.GetSection(nameof(KafkaCluster)).Get<KafkaCluster>();
                var messageSettings = context.Configuration.Get<KafkaMessage>();
                services.AddKafka(builder =>
                {
                    builder.AddCluster(cb =>
                    {
                        cb.WithBrokers(new[] { clusterSettings.BootstrapServers });
                        cb.AddProducer<KafkaFlowSender>(
                            pb => { pb.DefaultTopic(messageSettings.Topic); });
                    });
                });
                services.AddSingleton<ISender, KafkaFlowSender>();

                ConfigureJsonPostman(services, messageSettings);
                services.AddHostedService<SenderBackgroundService>();
            })
            .Build()
            .RunAsync();
    }

    private static void ConfigureJsonPostman(IServiceCollection services,
        KafkaMessage kafkaMessageSettings)
    {
        services.AddSingleton<IJsonPostman>(ctx =>
        {
            var protobufCompiler =
                new ProtobufCompiler.ProtobufCompiler(
                    ctx.GetRequiredService<ILogger<ProtobufCompiler.ProtobufCompiler>>());

            var protoFilePath = kafkaMessageSettings.ProtoPath;
            var protoString = File.ReadAllText(protoFilePath);

            var protobufAssembly = protobufCompiler.CompileProtobufAsync(protoString, CancellationToken.None)
                .GetAwaiter().GetResult();

            // TODO: get this type by name
            var messageType = protobufAssembly.MessageTypes.First();

            var postmanType = typeof(ProtobufJsonPostman<>).MakeGenericType(messageType);

            var sender = ctx.GetRequiredService<ISender>();

            var loggerType = typeof(ILogger<>).MakeGenericType(postmanType);
            var logger = ctx.GetRequiredService(loggerType);
            return Activator.CreateInstance(postmanType, sender, logger) as IJsonPostman ??
                   throw new ArgumentException("Can't create json postman instance");
        });
    }
}