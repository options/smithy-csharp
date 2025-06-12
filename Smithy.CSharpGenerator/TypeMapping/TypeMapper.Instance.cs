using Smithy.Model;
using Smithy.CSharpGenerator.Utils;

namespace Smithy.CSharpGenerator.TypeMapping;

public class TypeMapper
{
    /// <summary>
    /// Map a Smithy shape to its corresponding C# type
    /// </summary>
    public string MapToType(Shape shape, bool isStreaming = false)
    {
        if (shape == null)
            return "void";
            
        string smithyType = shape.Id;
        
        // Check for streaming trait
        if (shape.ConstraintTraits != null && shape.ConstraintTraits.Any(t => t.Name == "streaming"))
            isStreaming = true;
            
        // If it's streaming, handle differently based on the type
        if (isStreaming)
        {
            if (smithyType == "Blob" || smithyType == "blob")
                return "IAsyncEnumerable<byte[]>";
            if (smithyType == "String" || smithyType == "string")
                return "IAsyncEnumerable<string>";
            return "Stream";
        }
        
        // Handle Smithy 2.0 specification-compliant type mapping
        switch (smithyType)
        {
            // Simple types from Smithy 2.0 spec
            case "String":
            case "string":
                return "string";
            case "Blob":
            case "blob":
                return "byte[]";
            case "Boolean":
            case "boolean":
                return "bool";
            case "Byte":
            case "byte":
                return "sbyte";
            case "Short":
            case "short":
                return "short";
            case "Integer":
            case "integer":
                return "int";
            case "Long":
            case "long":
                return "long";
            case "Float":
            case "float":
                return "float";
            case "Double":
            case "double":
                return "double";
            case "BigInteger":
            case "bigInteger":
                return "System.Numerics.BigInteger";
            case "BigDecimal":
            case "bigDecimal":
                return "decimal";
            case "Timestamp":
            case "timestamp":
                return "DateTimeOffset";
            case "Document":
            case "document":
                return "System.Text.Json.JsonElement";
                
            // Smithy 2.0 primitive types
            case "PrimitiveBoolean":
                return "bool";
            case "PrimitiveByte":
                return "sbyte";
            case "PrimitiveShort":
                return "short";
            case "PrimitiveInteger":
                return "int";
            case "PrimitiveLong":
                return "long";
            case "PrimitiveFloat":
                return "float";
            case "PrimitiveDouble":
                return "double";
                
            // Unit type - Smithy 2.0 spec compliant
            case "Unit":
            case "smithy.api#Unit":
                return "void";
                
            // Default case for custom types
            default:
                return NamingUtils.PascalCase(smithyType);
        }
    }
}
