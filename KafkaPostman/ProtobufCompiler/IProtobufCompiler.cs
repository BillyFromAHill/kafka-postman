using System.Reflection;
using KafkaPostman.ProtobufAssemblyProvider;

namespace KafkaPostman.ProtobufCompiler;

public interface IProtobufCompiler
{
    public Task<ProtobufAssembly> CompileProtobufAsync(string protoSchema, CancellationToken ct);
}