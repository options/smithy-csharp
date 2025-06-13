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
ISmithyModelParser parser;
if (inputPath != null && inputPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    parser = new SmithyJsonAstParser();
else
    parser = new SmithyModelParser();
ISmithyModelValidator validator = new SmithyModelValidator();

// Parse and validate the model
SmithyModel model = parser.Parse(modelText);
List<string> errors = validator.Validate(model);

// Display namespace and shapes information
Console.WriteLine($"Model namespace: {model.Namespace}");
Console.WriteLine($"Found {model.Shapes.Count} shapes.");

if (errors.Count == 0)
{
    Console.WriteLine("Smithy 모델이 유효합니다.");
}
else
{
    Console.WriteLine("Smithy 모델 오류:");
    foreach (var error in errors)
        Console.WriteLine("- " + error);
}

Console.WriteLine("파싱된 Shape 목록:");
foreach (var shape in model.Shapes)
{
    Console.WriteLine($"- {shape.GetType().Name}: {shape.Id}");
}

// operation, structure도 샘플 입력에 맞게 파싱되는지 확인
Console.WriteLine("파싱된 Shape 목록 (추가 정보):");
foreach (var shape in model.Shapes)
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

// Code generation using the new V2 generator
Console.WriteLine("Generating code with the updated generator...");
var generator = new CSharpCodeGeneratorV2();
string generatedCode = generator.Generate(model);

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
    var svc = model.Shapes.OfType<ServiceShape>().FirstOrDefault();
    var serviceName = svc != null ? svc.Id : "Generated";
    finalOutputPath = Path.Combine(outputDirectory, serviceName + ".cs");
}
else
{
    // No directory specified, use current directory
    var svc = model.Shapes.OfType<ServiceShape>().FirstOrDefault();
    var serviceName = svc != null ? svc.Id : "Generated";
    finalOutputPath = Path.Combine(Environment.CurrentDirectory, serviceName + ".cs");
}

// Save the generated code
System.IO.File.WriteAllText(finalOutputPath, generatedCode);
Console.WriteLine($"C# code generated successfully to: {finalOutputPath}");
