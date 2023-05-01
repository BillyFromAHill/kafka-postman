using System.Diagnostics;
using System.Reflection;
using System.Text;
using KafkaPostman.ProtobufAssemblyProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

namespace KafkaPostman.ProtobufCompiler;

public class ProtobufCompiler : IProtobufCompiler
{
    private readonly ILogger<ProtobufCompiler> _logger;

    public ProtobufCompiler(ILogger<ProtobufCompiler> logger)
    {
        _logger = logger;
    }

    public async Task<ProtobufAssembly> CompileProtobufAsync(string protoSchema, CancellationToken ct)
    {
        var csharpCode = await GenerateCSharpSources(protoSchema, ct);

        var protoAssembly = await CompileCSharpSources(csharpCode, ct);

        return new ProtobufAssembly(protoAssembly);
    }

    private static bool AddAssembly(string assemblyDll, HashSet<PortableExecutableReference> references)
    {
        if (string.IsNullOrEmpty(assemblyDll)) return false;

        var file = Path.GetFullPath(assemblyDll);

        if (!File.Exists(file))
        {
            // check framework or dedicated runtime app folder
            var path = Path.GetDirectoryName(typeof(object).Assembly.Location);
            file = Path.Combine(path, assemblyDll);
            if (!File.Exists(file))
                return false;
        }

        if (references.Any(r => r.FilePath == file)) return true;

        try
        {
            var reference = MetadataReference.CreateFromFile(file);
            references.Add(reference);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static void AddAssemblies(HashSet<PortableExecutableReference> references, params string[] assemblies)
    {
        foreach (var file in assemblies)
            AddAssembly(file, references);
    }

    private static bool AddAssembly(Type type, HashSet<PortableExecutableReference> references)
    {
        try
        {
            if (references.Any(r => r.FilePath == type.Assembly.Location))
                return true;

            var systemReference = MetadataReference.CreateFromFile(type.Assembly.Location);
            references.Add(systemReference);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private async Task<Assembly> CompileCSharpSources(string csharpCode, CancellationToken ct)
    {
        var references = new HashSet<PortableExecutableReference>();
        AddNetCoreDefaultReferences(references);
        AddAssembly(typeof(Google.Protobuf.Extension), references);
        var tree = SyntaxFactory.ParseSyntaxTree(csharpCode.Trim(), cancellationToken: ct);
        var compilation = CSharpCompilation.Create("Executor.cs")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release))
            .WithReferences(references)
            .AddSyntaxTrees(tree);

        string errorMessage = null;
        Assembly assembly = null;

        bool isFileAssembly = false;
        Stream codeStream = null;
        await using (codeStream = new MemoryStream())
        {
            // Actually compile the code
            EmitResult compilationResult = null;
            compilationResult = compilation.Emit(codeStream);

            // Compilation Error handling
            if (!compilationResult.Success)
            {
                var sb = new StringBuilder();
                foreach (var diag in compilationResult.Diagnostics)
                {
                    sb.AppendLine(diag.ToString());
                }

                errorMessage = sb.ToString();
                
                _logger.LogError(errorMessage + csharpCode);

                // TODO: replace with a proper exception
                throw new ArgumentException(errorMessage);
            }

            assembly = Assembly.Load(((MemoryStream)codeStream).ToArray());
            return assembly;
        }
    }

    private static void AddNetCoreDefaultReferences(HashSet<PortableExecutableReference> references)
    {
        var rtPath = Path.GetDirectoryName(typeof(object).Assembly.Location) +
                     Path.DirectorySeparatorChar;

        AddAssemblies(
            references,
            rtPath + "System.Private.CoreLib.dll",
            rtPath + "System.Runtime.dll",
            rtPath + "System.Console.dll",
            rtPath + "netstandard.dll",
            rtPath + "Microsoft.CSharp.dll"
        );
    }

    private async Task<string> GenerateCSharpSources(string protoSchema, CancellationToken cancellationToken)
    {
        var tempPath = $"{Path.GetTempPath()}KafkaPostman\\{Guid.NewGuid()}";
        Directory.CreateDirectory(tempPath);
        var protoFileName = Path.Combine(tempPath, $"Proto_{Guid.NewGuid()}.proto");

        await File.WriteAllTextAsync(protoFileName, protoSchema, cancellationToken);
        var codeDirectory = $"{tempPath}\\cs";
        Directory.CreateDirectory(codeDirectory);

        // TODO: Check if protoc available
        var protocStartInfo = new ProcessStartInfo("protoc",
            $"{protoFileName} --proto_path=\"{tempPath}\" --csharp_out=\"{codeDirectory}\"")
        {
            RedirectStandardError = true
        };

        var process = Process.Start(protocStartInfo);
        if (process is null)
        {
            Directory.Delete(tempPath);
            throw new ArgumentException("Can't start code generation process.");
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var outputError = await process.StandardError.ReadToEndAsync(cancellationToken);
            _logger.LogError($"Code generation has finished with non-zero code. Error: {outputError}");
            throw new ArgumentException("Can't compile proto schema. See log for details.");
        }

        return await File.ReadAllTextAsync(Directory.GetFiles(codeDirectory).First(), cancellationToken);
    }
}