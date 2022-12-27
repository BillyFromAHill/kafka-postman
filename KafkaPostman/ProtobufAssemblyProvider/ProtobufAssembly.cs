using System.Reflection;

namespace KafkaPostman.ProtobufAssemblyProvider;

public class ProtobufAssembly
{
    public ProtobufAssembly(Assembly typesAssembly)
    {
        TypesAssembly = typesAssembly;
        
        // TODO: Extract proto types here
    }
    
    public Assembly TypesAssembly { get; }
    
    public IEnumerable<Type> MessageTypes { get; } 
}