using System.Text.Json;
using SMSR.CSharpAnalysis;

try
{
    if (args.SequenceEqual(new[] { "--self-test" }))
    {
        SemanticSelfCheck.Run();
        FlowSelfCheck.Run();
        ConnectionSelfCheck.Run();
        DefinitionSelfCheck.Run();
        ControlSelfCheck.Run();
        ValueSelfCheck.Run();
        ReturnSelfCheck.Run();
        ArgumentOriginsSelfCheck.Run();
        Console.WriteLine("C# semantic input-bundle self-check passed");
        return 0;
    }
    if (args.Length != 0 && !args.SequenceEqual(new[] { "--index" }))
        throw new ArgumentException("Unsupported command arguments");
    using var input = Console.OpenStandardInput();
    using var buffer = new MemoryStream();
    var chunk = new byte[8192];
    int read;
    while ((read = await input.ReadAsync(chunk)) != 0)
    {
        if (buffer.Length + read > 32 * 1024 * 1024) throw new ArgumentException("Input limit exceeded");
        buffer.Write(chunk, 0, read);
    }
    var request = JsonSerializer.Deserialize<AnalysisInput>(buffer.ToArray(), Contracts.Json)
        ?? throw new ArgumentException("Missing input");
    object result = args.Length == 0 ? Analyzer.Run(request) : FileCallIndex.Run(request);
    var bytes = JsonSerializer.SerializeToUtf8Bytes(result, Contracts.Json);
    if (bytes.Length > 16 * 1024 * 1024) throw new ArgumentException("Output limit exceeded");
    await Console.OpenStandardOutput().WriteAsync(bytes);
    return 0;
}
catch (Exception error)
{
    if (args.SequenceEqual(new[] { "--self-test" })) Console.Error.WriteLine(error);
    // Compiler exception text can quote source, identifiers or local machine paths.
    Console.WriteLine("{\"error\":\"CSharp analysis failed: check input and limits\"}");
    return 1;
}
