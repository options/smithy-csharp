using Smithy.Model;
using Smithy.CSharpGenerator.Formatters;
using Smithy.CSharpGenerator.Utils;
using Smithy.CSharpGenerator.TypeMapping;
using Smithy.CSharpGenerator.Generators;
using System.Text;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Smithy.CSharpGenerator;

public partial class CSharpCodeGenerator
{
    private readonly AttributeFormatter _attributeFormatter = new();
    private readonly ConstraintAttributeGenerator _constraintAttributeGenerator = new();
    private readonly HttpProtocolGenerator _httpProtocolGenerator = new();
    
    // Initialize these in constructor to avoid partial class conflicts
    public CSharpCodeGenerator()
    {
        // The code generation dependencies are initialized here
    }

    private string FormatConstraintTraits(IEnumerable<ConstraintTrait> traits)
    {
        if (traits == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var trait in traits)
        {
            sb.AppendLine($"// {trait}");
        }
        return sb.ToString();
    }    private string FormatConstraintAttributes(IEnumerable<ConstraintTrait> traits)
    {
        return _constraintAttributeGenerator.FormatConstraintAttributes(traits);
    }

    private string FormatAuthAttributes(IEnumerable<ConstraintTrait> traits)
    {
        if (traits == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var trait in traits)
        {
            switch (trait.Name)
            {
                case "auth":
                case "httpBasicAuth":
                case "httpBearerAuth":
                case "aws.v4Auth":
                case "optionalAuth":
                case "unauthenticated":
                    sb.AppendLine($"// Auth trait: @{trait.Name}{(trait.Properties.Count > 0 ? "(" + string.Join(", ", trait.Properties.Select(kv => $"{kv.Key}={kv.Value}")) + ")" : "")}");
                    // 예시: [Authorize] attribute로도 출력 가능
                    if (trait.Name == "auth" || trait.Name == "httpBasicAuth" || trait.Name == "httpBearerAuth" || trait.Name == "aws.v4Auth")
                        sb.AppendLine("[Authorize]");
                    if (trait.Name == "unauthenticated")
                        sb.AppendLine("[AllowAnonymous]");
                    break;
            }
        }
        return sb.ToString();
    }

    private string FormatProtocolAttributes(IEnumerable<ConstraintTrait> traits)
    {
        if (traits == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var trait in traits)
        {
            switch (trait.Name)
            {
                case "restJson1":
                case "restXml":
                case "awsJson1_0":
                case "awsJson1_1":
                case "awsQuery":
                case "ec2Query":
                case "aws.protocols#restJson1":
                case "aws.protocols#restXml":
                case "aws.protocols#awsJson1_0":
                case "aws.protocols#awsJson1_1":
                case "aws.protocols#awsQuery":
                case "aws.protocols#ec2Query":
                    sb.AppendLine($"// Protocol: @{trait.Name}");
                    sb.AppendLine($"[Protocol(\"{trait.Name}\")]");
                    break;
            }
        }
        return sb.ToString();
    }

    private string PascalCase(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        var parts = id.Split(new[] { '.', '#', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpper(part[0]));
            if (part.Length > 1)
                sb.Append(part.Substring(1));
        }
        return sb.ToString();
    }    private string MapType(string smithyType, bool isStreaming = false, string? streamingTarget = null)
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
            "Document" or "document" => "System.Text.Json.JsonElement", // Better mapping for document
            
            // Smithy 2.0 primitive types
            "PrimitiveBoolean" => "bool",
            "PrimitiveByte" => "sbyte", 
            "PrimitiveShort" => "short",
            "PrimitiveInteger" => "int",
            "PrimitiveLong" => "long",
            "PrimitiveFloat" => "float",
            "PrimitiveDouble" => "double",
            
            // Unit type - Smithy 2.0 spec compliant
            "Unit" or "smithy.api#Unit" => "void", // Better for operations
            
            // Default case for custom types
            _ => PascalCase(smithyType)
        };
    }    private string GenerateUnion(UnionShape union)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"public record {NamingUtils.PascalCase(union.Id)}");
        sb.AppendLine("{");
        foreach (var member in union.Members)
        {
            sb.AppendLine($"    public {TypeMapper.MapType(member.Target)} {NamingUtils.PascalCase(member.Name)} {{ get; init; }}");
        }
        sb.AppendLine("}");
        sb.AppendLine();
        return sb.ToString();
    }private string GenerateError(StructureShape shape)
    {
        var sb = new StringBuilder();
        var errorTrait = shape.ConstraintTraits.FirstOrDefault(t => t.Name == "error");
        var errorType = errorTrait?.Properties.GetValueOrDefault("value", "client")?.ToString() ?? "client";
        
        // Generate appropriate base exception type based on error trait
        var baseException = errorType.ToLower() switch
        {
            "client" => "ArgumentException",
            "server" => "InvalidOperationException", 
            _ => "Exception"
        };
        
        sb.Append(FormatConstraintAttributes(shape.ConstraintTraits));
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// {shape.Documentation ?? $"Exception for {shape.Id}"}");
        sb.AppendLine($"/// Error type: {errorType}");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class {PascalCase(shape.Id)}Exception : {baseException}");
        sb.AppendLine("{");
        
        // Constructors
        sb.AppendLine($"    public {PascalCase(shape.Id)}Exception() : base(\"{PascalCase(shape.Id)} error occurred\") {{ }}");
        sb.AppendLine($"    public {PascalCase(shape.Id)}Exception(string message) : base(message) {{ }}");
        sb.AppendLine($"    public {PascalCase(shape.Id)}Exception(string message, Exception innerException) : base(message, innerException) {{ }}");
        sb.AppendLine();
        
        // Properties from structure members
        foreach (var member in shape.Members)
        {
            sb.Append(FormatConstraintAttributes(member.ConstraintTraits));
            var memberType = MapType(member.Target);
            var memberName = PascalCase(member.Name);
            
            if (!string.IsNullOrEmpty(member.Documentation))
            {
                sb.AppendLine($"    /// <summary>{member.Documentation}</summary>");
            }
            
            sb.AppendLine($"    public {memberType} {memberName} {{ get; set; }}");
        }
        
        sb.AppendLine("}");
        sb.AppendLine();
        return sb.ToString();
    }

    private string GenerateEventStream(StructureShape shape)
    {
        // Assume shape is the event union
        return $"public IAsyncEnumerable<{PascalCase(shape.Id)}> {PascalCase(shape.Id)}Stream {{ get; set; }}\n";
    }    private string FormatHttpAttributes(IEnumerable<ConstraintTrait> traits)
    {
        if (traits == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var trait in traits)
        {
            if (trait.Name == "http")
            {
                var method = trait.Properties.TryGetValue("method", out var m) && m != null ? m.ToString() : "GET";
                var uri = trait.Properties.TryGetValue("uri", out var u) && u != null ? u.ToString() : "/";
                var code = trait.Properties.TryGetValue("code", out var c) && c != null ? c.ToString() : null;
                
                var methodCased = !string.IsNullOrEmpty(method) ? char.ToUpper(method[0]) + method.Substring(1).ToLower() : "Get";
                sb.AppendLine($"[Http{methodCased}(\"{uri}\")]");
                
                if (!string.IsNullOrEmpty(code))
                {
                    sb.AppendLine($"[ProducesResponseType({code})]");
                }
            }
            else if (trait.Name == "httpError")
            {
                if (trait.Properties.TryGetValue("code", out var errorCode) && errorCode != null)
                {
                    sb.AppendLine($"[ProducesResponseType(typeof(ProblemDetails), {errorCode})]");
                }
            }
            else if (trait.Name == "cors")
            {
                sb.AppendLine("[EnableCors]");
                if (trait.Properties.TryGetValue("origin", out var origin) && origin != null)
                {
                    sb.AppendLine($"// CORS Origin: {origin}");
                }
                if (trait.Properties.TryGetValue("maxAge", out var maxAge) && maxAge != null)
                {
                    sb.AppendLine($"// CORS MaxAge: {maxAge}");
                }
            }
        }
        return sb.ToString();
    }    private string FormatHttpParameter(MemberShape member)
    {
        foreach (var trait in member.ConstraintTraits)
        {
            switch (trait.Name)
            {
                case "httpLabel":
                    // Path parameter
                    var labelName = trait.Properties.TryGetValue("name", out var ln) && ln != null ? ln.ToString() : member.Name;
                    return $"[FromRoute(Name = \"{labelName}\")] ";
                    
                case "httpQuery":
                    // Query parameter
                    var queryName = trait.Properties.TryGetValue("name", out var qn) && qn != null ? qn.ToString() : member.Name;
                    return $"[FromQuery(Name = \"{queryName}\")] ";
                    
                case "httpHeader":
                    // Header parameter
                    var headerName = trait.Properties.TryGetValue("name", out var hn) && hn != null ? hn.ToString() : member.Name;
                    return $"[FromHeader(Name = \"{headerName}\")] ";
                    
                case "httpPayload":
                    // Body payload
                    return $"[FromBody] ";
                    
                case "httpPrefixHeaders":
                    // Prefix headers (e.g., x-amz-meta-)
                    var prefix = trait.Properties.TryGetValue("prefix", out var p) && p != null ? p.ToString() : member.Name;
                    return $"[FromHeader(Name = \"{prefix}*\")] ";
                    
                case "httpQueryParams":
                    // All query parameters as a map
                    return $"[FromQuery] ";
                    
                case "httpResponseCode":
                    // HTTP response code (for output)
                    return $"// HTTP Response Code member ";
            }
        }
        return string.Empty;
    }

    private string FormatEndpointAttributes(IEnumerable<ConstraintTrait> traits)
    {
        if (traits == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var trait in traits)
        {
            if (trait.Name == "endpoint")
            {
                // @endpoint trait: endpoint URL, hostPrefix, etc.
                if (trait.Properties.TryGetValue("hostPrefix", out var hostPrefix) && hostPrefix != null)
                {
                    sb.AppendLine($"// Endpoint hostPrefix: {hostPrefix}");
                    sb.AppendLine($"[EndpointHostPrefix(\"{hostPrefix}\")]\n");
                }
                if (trait.Properties.TryGetValue("url", out var url) && url != null)
                {
                    sb.AppendLine($"// Endpoint url: {url}");
                    sb.AppendLine($"[EndpointUrl(\"{url}\")]\n");
                }
            }
        }
        return sb.ToString();
    }

    private string FormatSelectorAttributes(IEnumerable<ConstraintTrait> traits)
    {
        if (traits == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var trait in traits)
        {
            if (trait.Name == "selector")
            {
                // Smithy selector trait: output as comment and custom attribute
                if (trait.Properties.TryGetValue("expression", out var expr) && expr != null)
                {
                    sb.AppendLine($"// Selector expression: {expr}");
                    sb.AppendLine($"[SmithySelector(\"{expr}\")]\n");
                }
            }
        }
        return sb.ToString();
    }

    private string FormatValidationAttributes(IEnumerable<ConstraintTrait> traits)
    {
        if (traits == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var trait in traits)
        {
            if (trait.Name == "validation")
            {
                // Smithy @validation trait: output as comment and custom attribute
                if (trait.Properties.TryGetValue("message", out var msg) && msg != null)
                {
                    sb.AppendLine($"// Validation message: {msg}");
                    sb.AppendLine($"[SmithyValidation(\"{msg}\")]\n");
                }
                if (trait.Properties.TryGetValue("severity", out var sev) && sev != null)
                {
                    sb.AppendLine($"// Validation severity: {sev}");
                }
                if (trait.Properties.TryGetValue("selector", out var sel) && sel != null)
                {
                    sb.AppendLine($"// Validation selector: {sel}");
                }
            }
        }
        return sb.ToString();
    }

    public string Generate(SmithyModel model)
    {
        var sb = new StringBuilder();
        var inner = new StringBuilder();
        // Smithy IDL: metadata → C# 주석
        if (model.Metadata != null && model.Metadata.Count > 0)
        {
            inner.AppendLine("// Smithy metadata");
            foreach (var kv in model.Metadata)
                inner.AppendLine($"// metadata {kv.Key} = {kv.Value}");
            inner.AppendLine();
        }
        // Smithy IDL: use → C# 주석
        if (model.Uses != null && model.Uses.Count > 0)
        {
            inner.AppendLine("// Smithy use");
            foreach (var u in model.Uses)
                inner.AppendLine($"// use {u}");
            inner.AppendLine();
        }
        // Smithy IDL: apply → C# 주석
        if (model.Applies != null && model.Applies.Count > 0)
        {
            inner.AppendLine("// Smithy apply");
            foreach (var a in model.Applies)
                inner.AppendLine($"// apply {a}");
            inner.AppendLine();
        }
        foreach (var shape in model.Shapes)
        {
            GenerateShape(shape, model, inner);
        }
        string ns = !string.IsNullOrWhiteSpace(model.Namespace) ? model.Namespace : "Generated";
        sb.AppendLine($"namespace {ns}");
        sb.AppendLine("{");
        sb.Append(inner.ToString());
        sb.AppendLine("}");
        return sb.ToString();
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
            case Shape s when s.ConstraintTraits?.Any(t => t.Name == "enum") == true:
                GenerateEnumFromTrait(s, inner);
                break;
            case UnionShape union:
                GenerateUnion(union, inner);
                break;
            case ListShape listShape:
                GenerateList(listShape, inner);
                break;
            case MapShape mapShape:
                GenerateMap(mapShape, inner);
                break;
            case SetShape setShape:
                GenerateSet(setShape, inner);
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
                // Operation shapes are handled as part of service generation
                // This case handles standalone operations
                inner.AppendLine($"// Operation {operationShape.Id} - handled as part of service");
                break;
            default:
                inner.AppendLine($"// Unsupported shape type: {shape.GetType().Name}");
                break;
        }
    }
      private void GenerateEnumFromTrait(Shape shape, StringBuilder inner)
    {
        var enumTrait = shape.ConstraintTraits.FirstOrDefault(t => t.Name == "enum");
        if (enumTrait == null) return;
        
        inner.Append(_attributeFormatter.FormatConstraintAttributes(shape.ConstraintTraits));
        inner.AppendLine($"public enum {NamingUtils.PascalCase(shape.Id)}");
        inner.AppendLine("{");
        
        if (enumTrait.Properties.TryGetValue("values", out var values) && values is List<Dictionary<string, object>> enumValues)
        {
            foreach (var enumValue in enumValues)
            {
                if (enumValue.TryGetValue("name", out var name))
                {
                    inner.AppendLine($"    {name},");
                }
            }
        }
        
        inner.AppendLine("}");
        inner.AppendLine();
    }
}
