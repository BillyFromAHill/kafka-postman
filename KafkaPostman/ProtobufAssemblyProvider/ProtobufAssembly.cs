using System.Reflection;

namespace KafkaPostman.ProtobufAssemblyProvider;

public class ProtobufAssembly
{
    public ProtobufAssembly(Assembly typesAssembly)
    {
        TypesAssembly = typesAssembly;

        // TODO: Right extract proto type here
        MessageTypes = typesAssembly.ExportedTypes.Skip(1).ToList();
    }
    
    public Assembly TypesAssembly { get; }
    
    public IEnumerable<Type> MessageTypes { get; } 
}