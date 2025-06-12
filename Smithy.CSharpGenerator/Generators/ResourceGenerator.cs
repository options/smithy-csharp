using Smithy.Model;
using Smithy.CSharpGenerator.Utils;
using Smithy.CSharpGenerator.TypeMapping;
using System.Text;

namespace Smithy.CSharpGenerator.Generators;

public class ResourceGenerator
{
    private readonly ConstraintAttributeGenerator _constraintAttributeGenerator = new();
    private readonly TypeMapper _typeMapper = new();
    
    public void GenerateResource(ResourceShape resourceShape, SmithyModel model, StringBuilder inner)
    {
        // Generate documentation comment if available
        if (!string.IsNullOrWhiteSpace(resourceShape.Documentation))
        {
            inner.AppendLine("/// <summary>");
            inner.AppendLine($"/// {resourceShape.Documentation}");
            inner.AppendLine("/// </summary>");
        }
        
        // Add constraint attributes
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(resourceShape.ConstraintTraits));
        
        // Generate resource class
        inner.AppendLine($"public class {NamingUtils.PascalCase(resourceShape.Id)}");
        inner.AppendLine("{");
        
        // Generate identifiers
        if (resourceShape.Identifiers != null)
        {
            inner.AppendLine("    // Resource identifiers");
            foreach (var identifier in resourceShape.Identifiers)
            {
                // Get target shape
                var targetShape = model.GetShape(identifier.Target);
                string typeName = _typeMapper.MapToType(targetShape);
                
                inner.AppendLine($"    public {typeName} {NamingUtils.PascalCase(identifier.Name)} {{ get; set; }}");
            }
            inner.AppendLine();
        }
        
        // Generate properties
        if (resourceShape.Properties != null)
        {
            inner.AppendLine("    // Resource properties");
            foreach (var property in resourceShape.Properties)
            {
                // Get target shape
                var targetShape = model.GetShape(property.Target);
                string typeName = _typeMapper.MapToType(targetShape);
                
                inner.AppendLine($"    public {typeName} {NamingUtils.PascalCase(property.Name)} {{ get; set; }}");
            }
            inner.AppendLine();
        }
        
        // Add resource lifecycle operations
        inner.AppendLine("    // Resource lifecycle operations");
        
        if (resourceShape.Create != null)
        {
            var createOp = model.GetShape(resourceShape.Create) as OperationShape;
            if (createOp != null)
                inner.AppendLine($"    // Create operation: {createOp.Id}");
        }
        
        if (resourceShape.Read != null)
        {
            var readOp = model.GetShape(resourceShape.Read) as OperationShape;
            if (readOp != null)
                inner.AppendLine($"    // Read operation: {readOp.Id}");
        }
        
        if (resourceShape.Update != null)
        {
            var updateOp = model.GetShape(resourceShape.Update) as OperationShape;
            if (updateOp != null)
                inner.AppendLine($"    // Update operation: {updateOp.Id}");
        }
        
        if (resourceShape.Delete != null)
        {
            var deleteOp = model.GetShape(resourceShape.Delete) as OperationShape;
            if (deleteOp != null)
                inner.AppendLine($"    // Delete operation: {deleteOp.Id}");
        }
        
        if (resourceShape.List != null)
        {
            var listOp = model.GetShape(resourceShape.List) as OperationShape;
            if (listOp != null)
                inner.AppendLine($"    // List operation: {listOp.Id}");
        }
        
        if (resourceShape.Operations?.Count > 0)
        {
            inner.AppendLine("    // Additional operations:");
            foreach (var opRef in resourceShape.Operations)
            {
                var op = model.GetShape(opRef) as OperationShape;
                if (op != null)
                    inner.AppendLine($"    // - {op.Id}");
            }
        }
        
        inner.AppendLine("}");
        inner.AppendLine();
    }
}
