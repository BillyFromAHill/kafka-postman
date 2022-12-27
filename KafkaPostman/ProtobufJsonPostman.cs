using Google.Protobuf;
using KafkaPostman.Kafka;
using Microsoft.Extensions.Logging;

namespace KafkaPostman;

public class ProtobufJsonPostman<TProtoModel> : IJsonPostman where TProtoModel : IMessage<TProtoModel>
{
    private readonly ISender _sender;
    private readonly ILogger<ProtobufJsonPostman<TProtoModel>> _logger;
    private readonly MessageParser<TProtoModel> _parser;

    public ProtobufJsonPostman(ISender sender, ILogger<ProtobufJsonPostman<TProtoModel>> logger)
    {
        _sender = sender;
        _logger = logger;

        var parserProperty = typeof(TProtoModel).GetProperty("Parser");
        if (parserProperty is null)
        {
            throw new ArgumentException($"{nameof(TProtoModel)} should have Parser property of type.");
        }

        _parser = (parserProperty.GetValue(null) as MessageParser<TProtoModel>)!;
        if (_parser is null)
        {
            throw new ArgumentNullException(parserProperty.Name);
        }
    }
    
    public async Task SendAsync(string json, CancellationToken ct)
    {
        using var scope = _logger.BeginScope("Send json.");
        try
        {
            _logger.LogInformation("Json is about to be published.");

            var message = _parser.ParseJson(json);
            await _sender.SendAsync(message.ToByteArray(), ct);

            _logger.LogInformation("Json is published.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured during the sending json.");
            throw;
        }
    }
}