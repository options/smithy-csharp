namespace Smithy.CSharpGenerator.TypeMapping
{
    // Keeping these static methods for backward compatibility
    public partial class TypeMapper
    {
        // Static methods that delegate to an instance
        private static readonly TypeMapper _instance = new TypeMapper();
    
        public static string MapType(string smithyType, bool isStreaming = false, string? streamingTarget = null)
        {
            if (isStreaming)
            {
                if (streamingTarget == "Blob" || streamingTarget == "blob")
                    return "IAsyncEnumerable<byte[]>";
                if (streamingTarget == "String" || streamingTarget == "string")
                    return "IAsyncEnumerable<string>";
                return "Stream";
            }
            
            // Handle Smithy 2.0 specification-compliant type mapping
            return smithyType switch
            {
                // Simple types from Smithy 2.0 spec
                "String" or "string" => "string",
                "Blob" or "blob" => "byte[]",
                "Boolean" or "boolean" => "bool",
                "Byte" or "byte" => "sbyte",
                "Short" or "short" => "short", 
                "Integer" or "integer" => "int",
                "Long" or "long" => "long",
                "Float" or "float" => "float",
                "Double" or "double" => "double",
                "BigInteger" or "bigInteger" => "System.Numerics.BigInteger",
                "BigDecimal" or "bigDecimal" => "decimal",
                "Timestamp" or "timestamp" => "DateTimeOffset",
                "Document" or "document" => "System.Text.Json.JsonElement",
                
                // Smithy 2.0 primitive types
                "PrimitiveBoolean" => "bool",
                "PrimitiveByte" => "sbyte", 
                "PrimitiveShort" => "short",
                "PrimitiveInteger" => "int",
                "PrimitiveLong" => "long",
                "PrimitiveFloat" => "float",
                "PrimitiveDouble" => "double",
                
                // Unit type - Smithy 2.0 spec compliant
                "Unit" or "smithy.api#Unit" => "void",
                
                // Default case for custom types
                _ => Utils.NamingUtils.PascalCase(smithyType)
            };
        }
    }
}
