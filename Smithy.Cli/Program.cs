using System;
using System.Collections.Generic;
using Smithy.Model;
using Smithy.CSharpGenerator;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

// 샘플 Smithy 모델 문자열 (실제 파서는 추후 구현)
string sampleSmithy = @"
service ExampleService { version: ""1.0"" operations: [ExampleOperation] }
operation ExampleOperation { input: ExampleInput output: ExampleOutput }
structure ExampleInput { foo: String, bar: Integer }
structure ExampleOutput { }
";

// 입력 파일 경로와 출력 파일 경로를 명령줄 인자로 받음
string? inputPath = args.Length > 0 && !args[0].StartsWith("-") ? args[0] : null;
string? outputPath = args.Length > 1 ? args[1] : null;
if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
{
    Console.WriteLine("Smithy.Cli - Smithy 2.0 to C# code generator");
    Console.WriteLine("Usage: Smithy.Cli <model.smithy|model.json> [output.cs]");
    Console.WriteLine("  <model.smithy|model.json> : Path to Smithy IDL or JSON AST file");
    Console.WriteLine("  [output.cs]               : (Optional) Output C# file name");
    Console.WriteLine("  --help, -h                : Show this help message");
    Console.WriteLine();
    Console.WriteLine("If no input file is provided, a sample Smithy model will be used.");
    return;
}
if (string.IsNullOrWhiteSpace(inputPath) || !System.IO.File.Exists(inputPath))
{
    Console.WriteLine("Usage: Smithy.Cli <model.smithy|model.json> [output.cs]");
    Console.WriteLine("No input file provided. Using sample Smithy model.");
    inputPath = null;
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

// Determine output filename if not provided
if (string.IsNullOrWhiteSpace(outputPath))
{
    // Use service name for the filename if available
    var svc = model.Shapes.OfType<ServiceShape>().FirstOrDefault();
    var serviceName = svc != null ? svc.Id : "Generated";
    outputPath = serviceName + ".cs";
}

// Save the generated code
System.IO.File.WriteAllText(outputPath, generatedCode);
Console.WriteLine($"C# code generated successfully to: {outputPath}");
