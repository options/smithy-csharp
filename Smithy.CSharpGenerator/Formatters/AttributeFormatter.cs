using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Smithy.CSharpGenerator.Formatters
{
    public class AttributeFormatter
    {
        public string FormatConstraintAttributes(IEnumerable<ConstraintTrait> traits)
        {
            if (traits == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var trait in traits)
            {
                switch (trait.Name)
                {
                    case "pattern":
                        if (trait.Properties.TryGetValue("regex", out var regex))
                            sb.AppendLine($"[RegularExpression(\"{regex}\")]");
                        break;
                    case "minLength":
                        if (trait.Properties.TryGetValue("min", out var minLen))
                            sb.AppendLine($"[MinLength({minLen})]");
                        break;
                    case "maxLength":
                        if (trait.Properties.TryGetValue("max", out var maxLen))
                            sb.AppendLine($"[MaxLength({maxLen})]");
                        break;
                    case "length":
                        if (trait.Properties.TryGetValue("min", out var minL))
                            sb.AppendLine($"[MinLength({minL})]");
                        if (trait.Properties.TryGetValue("max", out var maxL))
                            sb.AppendLine($"[MaxLength({maxL})]");
                        break;
                    case "range":
                        if (trait.Properties.TryGetValue("min", out var minR) && trait.Properties.TryGetValue("max", out var maxR))
                            sb.AppendLine($"[Range({minR}, {maxR})]");
                        break;
                    case "streaming":
                        sb.AppendLine("[Streaming]");
                        break;
                    case "required":
                        sb.AppendLine("[Required]");
                        break;
                    case "deprecated":
                        if (trait.Properties.TryGetValue("message", out var depMsg))
                            sb.AppendLine($"[Obsolete(\"{depMsg}\")]");
                        else
                            sb.AppendLine("[Obsolete]");
                        break;
                    case "sensitive":
                        sb.AppendLine("[Sensitive]");
                        break;
                    case "default":
                        if (trait.Properties.TryGetValue("value", out var defaultVal))
                            sb.AppendLine($"// Default value: {defaultVal}");
                        break;
                    case "sparse":
                        sb.AppendLine("// Sparse collection");
                        break;
                    case "uniqueItems":
                        sb.AppendLine("// Unique items constraint");
                        break;
                    case "error":
                        if (trait.Properties.TryGetValue("value", out var errorType))
                            sb.AppendLine($"// Error type: {errorType}");
                        else
                            sb.AppendLine("// Error structure");
                        break;
                    case "readonly":
                        sb.AppendLine("// Read-only operation");
                        break;
                    case "idempotent":
                        sb.AppendLine("// Idempotent operation");
                        break;
                    case "retryable":
                        sb.AppendLine("// Retryable operation");
                        if (trait.Properties.TryGetValue("throttling", out var throttling))
                            sb.AppendLine($"// Throttling: {throttling}");
                        break;
                    case "input":
                        sb.AppendLine("// Input structure for operation");
                        break;
                    case "output":
                        sb.AppendLine("// Output structure for operation");
                        break;
                    case "clientOptional":
                        sb.AppendLine("// Client-optional member");
                        break;
                    case "addedDefault":
                        sb.AppendLine("// Added default value for backward compatibility");
                        break;
                }
            }
            return sb.ToString();
        }

        public string FormatAuthAttributes(IEnumerable<ConstraintTrait> traits)
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
                        if (trait.Name == "auth" || trait.Name == "httpBasicAuth" || trait.Name == "httpBearerAuth" || trait.Name == "aws.v4Auth")
                            sb.AppendLine("[Authorize]");
                        if (trait.Name == "unauthenticated")
                            sb.AppendLine("[AllowAnonymous]");
                        break;
                }
            }
            return sb.ToString();
        }

        public string FormatProtocolAttributes(IEnumerable<ConstraintTrait> traits)
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

        public string FormatHttpAttributes(IEnumerable<ConstraintTrait> traits)
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
        }
    }
}
