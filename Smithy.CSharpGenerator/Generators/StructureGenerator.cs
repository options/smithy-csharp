using Smithy.Model;
using Smithy.CSharpGenerator.Utils;
using Smithy.CSharpGenerator.TypeMapping;
using System.Text;

namespace Smithy.CSharpGenerator.Generators;

public class StructureGenerator
{
    private readonly ConstraintAttributeGenerator _constraintAttributeGenerator = new();
    private readonly TypeMapper _typeMapper = new();
    
    public void GenerateStructure(StructureShape structureShape, SmithyModel model, StringBuilder inner)
    {
        // Check for error trait to determine if it's an exception
        bool isException = structureShape.ConstraintTraits.Any(t => t.Name == "error");
        string baseType = "class";
        string baseClass = string.Empty;
        
        if (isException)
        {
            baseClass = " : Exception";
              // Determine which type of exception based on error trait value
            var errorTrait = structureShape.ConstraintTraits.FirstOrDefault(t => t.Name == "error");
            if (errorTrait?.Properties.TryGetValue("value", out var errorType) == true)
            {
                string? errorTypeStr = errorType?.ToString()?.ToLowerInvariant();
                if (errorTypeStr == "client")
                    baseClass = " : ArgumentException";
                else if (errorTypeStr == "server")
                    baseClass = " : InvalidOperationException";
            }
        }
        
        // Generate documentation comment if available
        if (!string.IsNullOrWhiteSpace(structureShape.Documentation))
        {
            inner.AppendLine("/// <summary>");
            inner.AppendLine($"/// {structureShape.Documentation}");
            inner.AppendLine("/// </summary>");
        }
        
        // Add constraint attributes
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(structureShape.ConstraintTraits));
        
        // Generate class definition
        inner.AppendLine($"public {baseType} {NamingUtils.PascalCase(structureShape.Id)}{baseClass}");
        inner.AppendLine("{");
        
        // Generate constructor for exceptions
        if (isException)
        {
            GenerateExceptionConstructors(structureShape, inner);
        }
        
        // Generate properties
        foreach (var member in structureShape.Members)
        {
            // Generate documentation for member if available
            if (!string.IsNullOrWhiteSpace(member.Documentation))
            {
                inner.AppendLine("    /// <summary>");
                inner.AppendLine($"    /// {member.Documentation}");
                inner.AppendLine("    /// </summary>");
            }
            
            // Add constraint attributes for member
            inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(member.ConstraintTraits)
                .Replace("\n", "\n    ")); // Indent attributes
            
            // Get target shape
            var targetShape = model.GetShape(member.Target);
            string typeName = _typeMapper.MapToType(targetShape);
            
            // Make nullable if not required
            if (!member.ConstraintTraits.Any(t => t.Name == "required"))
            {
                typeName = $"{typeName}?";
            }
            
            // Generate property
            inner.AppendLine($"    public {typeName} {NamingUtils.PascalCase(member.Name)} {{ get; set; }}");
            inner.AppendLine();
        }
        
        inner.AppendLine("}");
        inner.AppendLine();
    }
    
    private void GenerateExceptionConstructors(StructureShape structureShape, StringBuilder inner)
    {
        string typeName = NamingUtils.PascalCase(structureShape.Id);
        
        inner.AppendLine($"    public {typeName}() : base() {{ }}");
        inner.AppendLine();
        
        inner.AppendLine($"    public {typeName}(string message) : base(message) {{ }}");
        inner.AppendLine();
        
        inner.AppendLine($"    public {typeName}(string message, Exception innerException) : base(message, innerException) {{ }}");
        inner.AppendLine();
    }
}
