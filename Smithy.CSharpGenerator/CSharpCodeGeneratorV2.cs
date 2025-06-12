using Smithy.Model;
using Smithy.CSharpGenerator.Formatters;
using Smithy.CSharpGenerator.Utils;
using Smithy.CSharpGenerator.TypeMapping;
using Smithy.CSharpGenerator.Generators;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Smithy.CSharpGenerator;

/// <summary>
/// The main Smithy to C# code generator with refactored components
/// </summary>
public class CSharpCodeGeneratorV2
{
    private readonly AttributeFormatter _attributeFormatter = new();
    private readonly ConstraintAttributeGenerator _constraintAttributeGenerator = new();
    private readonly HttpProtocolGenerator _httpProtocolGenerator = new();
    private readonly ServiceGenerator _serviceGenerator = new();
    private readonly StructureGenerator _structureGenerator = new();
    private readonly ResourceGenerator _resourceGenerator = new();
    private readonly TypeMapper _typeMapper = new TypeMapper();

    /// <summary>
    /// Generate C# code from a Smithy model
    /// </summary>
    public string Generate(SmithyModel model)
    {
        var sb = new StringBuilder();
        var inner = new StringBuilder();
        
        // Generate metadata comments
        GenerateMetadataComments(model, inner);
        
        // Generate using statements
        GenerateUsingStatements(inner);
        
        // Generate shapes
        foreach (var shape in model.Shapes)
        {
            GenerateShape(shape, model, inner);
        }
        
        // Wrap in namespace
        string ns = !string.IsNullOrWhiteSpace(model.Namespace) ? model.Namespace : "Generated";
        sb.AppendLine($"namespace {ns}");
        sb.AppendLine("{");
        sb.Append(inner.ToString());
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private void GenerateMetadataComments(SmithyModel model, StringBuilder inner)
    {
        if (model.Metadata?.Count > 0)
        {
            inner.AppendLine("// Smithy metadata");
            foreach (var kv in model.Metadata)
                inner.AppendLine($"// metadata {kv.Key} = {kv.Value}");
            inner.AppendLine();
        }

        if (model.Uses?.Count > 0)
        {
            inner.AppendLine("// Smithy use");
            foreach (var u in model.Uses)
                inner.AppendLine($"// use {u}");
            inner.AppendLine();
        }

        if (model.Applies?.Count > 0)
        {
            inner.AppendLine("// Smithy apply");
            foreach (var a in model.Applies)
                inner.AppendLine($"// apply {a}");
            inner.AppendLine();
        }
    }

    private void GenerateUsingStatements(StringBuilder inner)
    {
        inner.AppendLine("using System;");
        inner.AppendLine("using System.Collections.Generic;");
        inner.AppendLine("using System.ComponentModel.DataAnnotations;");
        inner.AppendLine("using System.Runtime.Serialization;");
        inner.AppendLine("using System.Text.Json;");
        inner.AppendLine("using System.Threading.Tasks;");
        inner.AppendLine("using Microsoft.AspNetCore.Cors;");
        inner.AppendLine("using Microsoft.AspNetCore.Mvc;");
        inner.AppendLine();
    }

    private void GenerateShape(Shape shape, SmithyModel model, StringBuilder inner)
    {
        // Generate documentation
        if (!string.IsNullOrWhiteSpace(shape.Documentation))
        {
            inner.AppendLine($"/// <summary>");
            inner.AppendLine($"/// {shape.Documentation}");
            inner.AppendLine($"/// </summary>");
        }

        switch (shape)
        {
            case EnumShape enumShape:
                GenerateEnum(enumShape, inner);
                break;
            case IntEnumShape intEnumShape:
                GenerateIntEnum(intEnumShape, inner);
                break;
            case UnionShape union:
                GenerateUnion(union, model, inner);
                break;
            case ListShape listShape:
                GenerateList(listShape, model, inner);
                break;
            case MapShape mapShape:
                GenerateMap(mapShape, model, inner);
                break;
            case SetShape setShape:
                GenerateSet(setShape, model, inner);
                break;
            case StructureShape structShape:
                _structureGenerator.GenerateStructure(structShape, model, inner);
                break;
            case ServiceShape serviceShape:
                _serviceGenerator.GenerateService(serviceShape, model, inner);
                break;
            case ResourceShape resourceShape:
                _resourceGenerator.GenerateResource(resourceShape, model, inner);
                break;
            case OperationShape operationShape:
                // Handled as part of service generation
                break;
            default:
                inner.AppendLine($"// Unsupported shape type: {shape.GetType().Name}");
                break;
        }
    }

    // The following methods handle generation of simple types
    
    private void GenerateEnum(EnumShape enumShape, StringBuilder inner)
    {
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(enumShape.ConstraintTraits));
        inner.AppendLine($"public enum {NamingUtils.PascalCase(enumShape.Id)}");
        inner.AppendLine("{");
        
        foreach (var member in enumShape.Members)
        {
            if (!string.IsNullOrWhiteSpace(member.Documentation))
            {
                inner.AppendLine($"    /// <summary>");
                inner.AppendLine($"    /// {member.Documentation}");
                inner.AppendLine($"    /// </summary>");
            }
            
            var enumValueTrait = member.ConstraintTraits.FirstOrDefault(t => t.Name == "enumValue");
            if (enumValueTrait != null && enumValueTrait.Properties.TryGetValue("value", out var enumValue))
            {
                inner.AppendLine($"    [EnumMember(Value = \"{enumValue}\")]");
            }
            
            if (member.ConstraintTraits.Any(t => t.Name == "deprecated"))
            {
                inner.AppendLine("    [Obsolete]");
            }
            
            inner.AppendLine($"    {NamingUtils.PascalCase(member.Name)},");
        }
        
        inner.AppendLine("}");
        inner.AppendLine();
    }

    private void GenerateIntEnum(IntEnumShape intEnumShape, StringBuilder inner)
    {
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(intEnumShape.ConstraintTraits));
        inner.AppendLine($"public enum {NamingUtils.PascalCase(intEnumShape.Id)} : int");
        inner.AppendLine("{");
        
        foreach (var member in intEnumShape.Members)
        {
            if (!string.IsNullOrWhiteSpace(member.Documentation))
            {
                inner.AppendLine($"    /// <summary>");
                inner.AppendLine($"    /// {member.Documentation}");
                inner.AppendLine($"    /// </summary>");
            }
            
            if (member.ConstraintTraits.Any(t => t.Name == "deprecated"))
            {
                inner.AppendLine("    [Obsolete]");
            }
            
            inner.AppendLine($"    {NamingUtils.PascalCase(member.Name)} = {member.Value},");
        }
        
        inner.AppendLine("}");
        inner.AppendLine();
    }

    private void GenerateUnion(UnionShape union, SmithyModel model, StringBuilder inner)
    {
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(union.ConstraintTraits));
        inner.AppendLine($"public record {NamingUtils.PascalCase(union.Id)}");
        inner.AppendLine("{");
        
        foreach (var member in union.Members)
        {
            if (!string.IsNullOrWhiteSpace(member.Documentation))
            {
                inner.AppendLine($"    /// <summary>");
                inner.AppendLine($"    /// {member.Documentation}");
                inner.AppendLine($"    /// </summary>");
            }
            
            var targetShape = model.GetShape(member.Target);
            string typeName = _typeMapper.MapToType(targetShape);
            inner.AppendLine($"    public {typeName}? {NamingUtils.PascalCase(member.Name)} {{ get; init; }}");
        }
        
        inner.AppendLine("}");
        inner.AppendLine();
    }

    private void GenerateList(ListShape listShape, SmithyModel model, StringBuilder inner)
    {
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(listShape.ConstraintTraits));
        
        // Get member target shape
        var memberTargetShape = model.GetShape(listShape.Member.Target);
        string memberTypeName = _typeMapper.MapToType(memberTargetShape);
        
        inner.AppendLine($"public class {NamingUtils.PascalCase(listShape.Id)} : List<{memberTypeName}>");
        inner.AppendLine("{");
        inner.AppendLine("}");
        inner.AppendLine();
    }

    private void GenerateMap(MapShape mapShape, SmithyModel model, StringBuilder inner)
    {
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(mapShape.ConstraintTraits));
        
        // Get key and value target shapes
        var keyTargetShape = model.GetShape(mapShape.Key.Target);
        var valueTargetShape = model.GetShape(mapShape.Value.Target);
        
        string keyTypeName = _typeMapper.MapToType(keyTargetShape);
        string valueTypeName = _typeMapper.MapToType(valueTargetShape);
        
        inner.AppendLine($"public class {NamingUtils.PascalCase(mapShape.Id)} : Dictionary<{keyTypeName}, {valueTypeName}>");
        inner.AppendLine("{");
        inner.AppendLine("}");
        inner.AppendLine();
    }

    private void GenerateSet(SetShape setShape, SmithyModel model, StringBuilder inner)
    {
        inner.Append(_constraintAttributeGenerator.FormatConstraintAttributes(setShape.ConstraintTraits));
        
        // Get member target shape
        var memberTargetShape = model.GetShape(setShape.Member.Target);
        string memberTypeName = _typeMapper.MapToType(memberTargetShape);
        
        inner.AppendLine($"public class {NamingUtils.PascalCase(setShape.Id)} : HashSet<{memberTypeName}>");
        inner.AppendLine("{");
        inner.AppendLine("}");
        inner.AppendLine();
    }
}
