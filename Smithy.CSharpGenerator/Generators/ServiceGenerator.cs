using Smithy.Model;
using Smithy.CSharpGenerator.Utils;
using Smithy.CSharpGenerator.TypeMapping;
using System.Text;

namespace Smithy.CSharpGenerator.Generators;

public class ServiceGenerator
{
    private readonly HttpProtocolGenerator _httpProtocolGenerator = new();
    private readonly ConstraintAttributeGenerator _constraintAttributeGenerator = new();
    private readonly TypeMapper _typeMapper = new();

    public void GenerateService(ServiceShape serviceShape, SmithyModel model, StringBuilder inner) 
    {
        // Generate service interface
        inner.AppendLine($"public interface I{NamingUtils.PascalCase(serviceShape.Id)}");
        inner.AppendLine("{");
        
        // Add service operations
        foreach (var op in serviceShape.Operations)
        {
            var operation = model.GetShape(op) as OperationShape;
            if (operation == null) continue;
            
            GenerateOperationSignature(operation, model, inner);
            inner.AppendLine();
        }
        
        inner.AppendLine("}");
        inner.AppendLine();
        
        // Generate service controller implementation
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(serviceShape.ConstraintTraits));
        inner.AppendLine("[ApiController]");
        
        // Route attribute with proper prefix
        var version = "";
        if (serviceShape.ConstraintTraits.FirstOrDefault(t => t.Name == "version")?.Properties.TryGetValue("value", out var versionValue) == true)
        {
            version = $"v{versionValue}";
        }
        
        inner.AppendLine($"[Route(\"api/{version}/{NamingUtils.KebabCase(serviceShape.Id)}\")]");
        inner.AppendLine($"public class {NamingUtils.PascalCase(serviceShape.Id)}Controller : ControllerBase, I{NamingUtils.PascalCase(serviceShape.Id)}");
        inner.AppendLine("{");
        
        // Add controller implementations
        foreach (var op in serviceShape.Operations)
        {
            var operation = model.GetShape(op) as OperationShape;
            if (operation == null) continue;
            
            GenerateOperationImplementation(operation, model, inner);
            inner.AppendLine();
        }
        
        inner.AppendLine("}");
    }
    
    private void GenerateOperationSignature(OperationShape operation, SmithyModel model, StringBuilder inner)
    {
        // Check for documentation
        if (!string.IsNullOrWhiteSpace(operation.Documentation))
        {
            inner.AppendLine("    /// <summary>");
            inner.AppendLine($"    /// {operation.Documentation}");
            inner.AppendLine("    /// </summary>");
        }
        
        // Get input/output types
        string inputType = "void";
        if (operation.Input != null)
        {
            var inputShape = model.GetShape(operation.Input);
            if (inputShape != null)
                inputType = _typeMapper.MapToType(inputShape);
        }
        
        string outputType = "void";
        if (operation.Output != null)
        {
            var outputShape = model.GetShape(operation.Output);
            if (outputShape != null)
                outputType = _typeMapper.MapToType(outputShape);
        }
        
        // Generate method signature
        if (outputType == "void")
        {
            inner.AppendLine($"    Task {NamingUtils.PascalCase(operation.Id)}({inputType} input);");
        }
        else
        {
            inner.AppendLine($"    Task<{outputType}> {NamingUtils.PascalCase(operation.Id)}({inputType} input);");
        }
    }
    
    private void GenerateOperationImplementation(OperationShape operation, SmithyModel model, StringBuilder inner)
    {
        // Get input/output types
        string inputType = "void";
        if (operation.Input != null)
        {
            var inputShape = model.GetShape(operation.Input);
            if (inputShape != null)
                inputType = _typeMapper.MapToType(inputShape);
        }
        
        string outputType = "void";
        if (operation.Output != null)
        {
            var outputShape = model.GetShape(operation.Output);
            if (outputShape != null)
                outputType = _typeMapper.MapToType(outputShape);
        }
        
        // Add constraint attributes
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(operation.ConstraintTraits));
        
        // Add HTTP protocol attributes
        var httpTrait = operation.ConstraintTraits.FirstOrDefault(t => t.Name == "http");
        inner.Append(_httpProtocolGenerator.FormatHttpTrait(httpTrait));
        
        // Add HTTP error attributes
        var httpErrorTrait = operation.ConstraintTraits.FirstOrDefault(t => t.Name == "httpError");
        inner.Append(_httpProtocolGenerator.FormatHttpErrorTrait(httpErrorTrait));
        
        // Generate method implementation
        if (outputType == "void")
        {
            inner.AppendLine($"    public Task {NamingUtils.PascalCase(operation.Id)}({inputType} input)");
            inner.AppendLine("    {");
            inner.AppendLine("        // TODO: Implement service operation");
            inner.AppendLine("        return Task.CompletedTask;");
            inner.AppendLine("    }");
        }
        else
        {
            inner.AppendLine($"    public Task<{outputType}> {NamingUtils.PascalCase(operation.Id)}({inputType} input)");
            inner.AppendLine("    {");
            inner.AppendLine("        // TODO: Implement service operation");
            inner.AppendLine($"        return Task.FromResult(new {outputType}());");
            inner.AppendLine("    }");
        }
    }
}
