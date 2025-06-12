using Smithy.Model;
using Smithy.CSharpGenerator.Utils;
using Smithy.CSharpGenerator.TypeMapping;
using System.Text;
using System.Linq;

namespace Smithy.CSharpGenerator;

public partial class CSharpCodeGenerator
{
    private void GenerateEnum(EnumShape enumShape, StringBuilder inner)
    {
        inner.Append(_attributeFormatter.FormatConstraintAttributes(enumShape.ConstraintTraits));
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
        inner.Append(_attributeFormatter.FormatConstraintAttributes(intEnumShape.ConstraintTraits));
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

    private void GenerateUnion(UnionShape union, StringBuilder inner)
    {
        inner.AppendLine($"public record {NamingUtils.PascalCase(union.Id)}");
        inner.AppendLine("{");
        foreach (var member in union.Members)
        {
            inner.AppendLine($"    public {TypeMapper.MapType(member.Target)} {NamingUtils.PascalCase(member.Name)} {{ get; init; }}");
        }
        inner.AppendLine("}");
        inner.AppendLine();
    }

    private void GenerateList(ListShape listShape, StringBuilder inner)
    {
        var itemType = TypeMapper.MapType(listShape.Member.Target);
        inner.AppendLine($"public class {NamingUtils.PascalCase(listShape.Id)} : List<{itemType}>");
        inner.AppendLine("{");
        inner.AppendLine("}");
        inner.AppendLine();
    }

    private void GenerateMap(MapShape mapShape, StringBuilder inner)
    {
        var keyType = TypeMapper.MapType(mapShape.Key.Target);
        var valueType = TypeMapper.MapType(mapShape.Value.Target);
        inner.AppendLine($"public class {NamingUtils.PascalCase(mapShape.Id)} : Dictionary<{keyType}, {valueType}>");
        inner.AppendLine("{");
        inner.AppendLine("}");
        inner.AppendLine();
    }

    private void GenerateSet(SetShape setShape, StringBuilder inner)
    {
        var itemType = TypeMapper.MapType(setShape.Member.Target);
        inner.AppendLine($"public class {NamingUtils.PascalCase(setShape.Id)} : HashSet<{itemType}>");
        inner.AppendLine("{");
        inner.AppendLine("}");
        inner.AppendLine();
    }
}
