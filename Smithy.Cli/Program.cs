using System;
using System.Collections.Generic;
using Smithy.Model;
using Smithy.CSharpGenerator;
using Smithy.Model.Parsers;
using Smithy.Model.Validation;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Smithy C# Code Generator");

// Helper methods for command-line usage
void ShowHelp()
{
    Console.WriteLine("Smithy.Cli - Smithy 2.0 to C# code generator");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("  smithy-cli generate <model.smithy|model.json> [options]");
    Console.WriteLine();
    Console.WriteLine("COMMANDS:");
    Console.WriteLine("  generate     Generate C# code from a Smithy model");
    Console.WriteLine();
    Console.WriteLine("ARGUMENTS:");
    Console.WriteLine("  <model.smithy|model.json>   Path to Smithy IDL or JSON AST file");
    Console.WriteLine();
    Console.WriteLine("OPTIONS:");
    Console.WriteLine("  -o, --output <dir>          Output directory for generated code");
    Console.WriteLine("  -h, --help                  Show this help message");
    Console.WriteLine("  -v, --version               Show version information");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("  smithy-cli generate path/to/model.smithy");
    Console.WriteLine("  smithy-cli generate path/to/model.smithy --output MyGeneratedCode");
}

void ShowVersion()
{
    var version = typeof(Program).Assembly.GetName().Version;
    Console.WriteLine($"smithy-cli version {version}");
}

// 샘플 Smithy 모델 문자열 (실제 파서는 추후 구현)
string sampleSmithy = @"
service ExampleService { version: ""1.0"" operations: [ExampleOperation] }
operation ExampleOperation { input: ExampleInput output: ExampleOutput }
structure ExampleInput { foo: String, bar: Integer }
structure ExampleOutput { }
";

// Process command line arguments
if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "help"))
{
    ShowHelp();
    return;
}

if (args.Length > 0 && args[0] == "generate")
{
    args = args.Skip(1).ToArray(); // Remove 'generate' command
}

// Check for --output or -o flag
string? inputPath = null;
string? outputDirectory = null;

for (int i = 0; i < args.Length; i++)
{
    if ((args[i] == "--output" || args[i] == "-o") && i + 1 < args.Length)
    {
        outputDirectory = args[i + 1];
        i++; // Skip the next argument since we've consumed it
    }
    else if (!args[i].StartsWith("-") && inputPath == null)
    {
        // First non-flag argument is the input file
        inputPath = args[i];
    }
}

if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
{
    ShowVersion();
    return;
}

if (string.IsNullOrWhiteSpace(inputPath))
{
    Console.WriteLine("No input file specified.");
    ShowHelp();
    return;
}

if (!System.IO.File.Exists(inputPath))
{
    Console.WriteLine($"Error: Input file not found: {inputPath}");
    return;
}
string modelText = inputPath != null ? System.IO.File.ReadAllText(inputPath) : sampleSmithy;

// Use the new enhanced parser with error recovery
var enhancedParser = new SmithyModelParserV2();
var parseResult = enhancedParser.ParseWithDiagnostics(modelText);

// Display parsing results with diagnostics
Console.WriteLine($"Model namespace: {parseResult.Model.Namespace}");
Console.WriteLine($"Found {parseResult.Model.Shapes.Count} shapes.");
Console.WriteLine($"Parse result: {(parseResult.IsSuccess ? "Success" : "Partial/Failed")}");
Console.WriteLine();

// Display diagnostics
if (parseResult.Diagnostics.Count > 0)
{
    Console.WriteLine("=== PARSING DIAGNOSTICS ===");
    
    // Group by severity
    var errors = parseResult.GetDiagnostics(DiagnosticSeverity.Error).ToList();
    var fatals = parseResult.GetDiagnostics(DiagnosticSeverity.Fatal).ToList();
    var warnings = parseResult.GetDiagnostics(DiagnosticSeverity.Warning).ToList();
    var infos = parseResult.GetDiagnostics(DiagnosticSeverity.Info).ToList();
    
    if (fatals.Count > 0)
    {
        Console.WriteLine($"\n🚫 FATAL ERRORS ({fatals.Count}):");
        foreach (var diagnostic in fatals)
        {
            Console.WriteLine($"  {diagnostic}");
        }
    }
    
    if (errors.Count > 0)
    {
        Console.WriteLine($"\n❌ ERRORS ({errors.Count}):");
        foreach (var diagnostic in errors)
        {
            Console.WriteLine($"  {diagnostic}");
        }
    }
    
    if (warnings.Count > 0)
    {
        Console.WriteLine($"\n⚠️  WARNINGS ({warnings.Count}):");
        foreach (var diagnostic in warnings)
        {
            Console.WriteLine($"  {diagnostic}");
        }
    }
    
    if (infos.Count > 0)
    {
        Console.WriteLine($"\nℹ️  INFO ({infos.Count}):");
        foreach (var diagnostic in infos)
        {
            Console.WriteLine($"  {diagnostic}");
        }
    }
    
    Console.WriteLine();
}

// Legacy validation for compatibility
ISmithyModelValidator validator = new SmithyModelValidator();
List<string> legacyErrors = validator.Validate(parseResult.Model);

if (legacyErrors.Count > 0)
{
    Console.WriteLine("=== LEGACY VALIDATION ERRORS ===");
    foreach (var error in legacyErrors)
        Console.WriteLine("- " + error);
    Console.WriteLine();
}

// Summary
if (parseResult.IsSuccess && legacyErrors.Count == 0)
{
    Console.WriteLine("✅ Smithy model is valid and ready for code generation.");
}
else if (!parseResult.HasFatalErrors)
{
    Console.WriteLine("⚠️  Smithy model has issues but partial code generation may be possible.");
}
else
{
    Console.WriteLine("❌ Smithy model has fatal errors - code generation not recommended.");
}

// Only proceed with code generation if we have a valid model
if (parseResult.IsSuccess || !parseResult.HasFatalErrors)
{
    Console.WriteLine("\n=== PARSED SHAPES ===");
    foreach (var shape in parseResult.Model.Shapes)
    {
        switch (shape)
        {
            case OperationShape op:
                Console.WriteLine($"- Operation: {op.Id}, Input: {op.Input}, Output: {op.Output}");
                break;
            case StructureShape str:
                Console.Write($"- Structure: {str.Id}, Members: {str.Members.Count}");
                if (str.Members.Count > 0)
                {
                    Console.Write(" [");
                    Console.Write(string.Join(", ", str.Members.Select(m => $"{m.Name}: {m.Target}")));
                    Console.Write("]");
                }
                Console.WriteLine();
                break;
            case ServiceShape svc:
                Console.WriteLine($"- Service: {svc.Id}, Operations: {svc.Operations.Count}");
                break;
            default:
                Console.WriteLine($"- {shape.GetType().Name}: {shape.Id}");
                break;
        }
    }

    // Code generation using the V2 generator
    Console.WriteLine("\n=== CODE GENERATION ===");
    var generator = new CSharpCodeGeneratorV2();
    string generatedCode = generator.Generate(parseResult.Model);

    // Determine output path
    string finalOutputPath;

    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        // Create output directory if it doesn't exist
        if (!System.IO.Directory.Exists(outputDirectory))
        {
            System.IO.Directory.CreateDirectory(outputDirectory);
        }
        
        // Use service name for the filename if available
        var svc = parseResult.Model.Shapes.OfType<ServiceShape>().FirstOrDefault();
        var serviceName = svc != null ? svc.Id : "Generated";
        finalOutputPath = Path.Combine(outputDirectory, serviceName + ".cs");
    }
    else
    {
        // No directory specified, use current directory
        var svc = parseResult.Model.Shapes.OfType<ServiceShape>().FirstOrDefault();
        var serviceName = svc != null ? svc.Id : "Generated";
        finalOutputPath = Path.Combine(Environment.CurrentDirectory, serviceName + ".cs");
    }

    // Save the generated code
    System.IO.File.WriteAllText(finalOutputPath, generatedCode);
    Console.WriteLine($"✅ C# code generated successfully to: {finalOutputPath}");
}
else
{
    Console.WriteLine("❌ Code generation skipped due to fatal parsing errors.");
}
