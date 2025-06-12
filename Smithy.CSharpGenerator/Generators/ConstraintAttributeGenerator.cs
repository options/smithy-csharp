using Smithy.Model;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Smithy.CSharpGenerator.Generators;

public class ConstraintAttributeGenerator
{
    public string FormatConstraintTraits(IEnumerable<ConstraintTrait> traits)
    {
        if (traits == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var trait in traits)
        {
            sb.AppendLine($"// {trait}");
        }
        return sb.ToString();
    }

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
                    // @error trait makes this an exception class
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
                case "httpLabel":
                    sb.AppendLine("[FromRoute]");
                    break;
                case "httpQuery":
                    if (trait.Properties.TryGetValue("name", out var queryName))
                        sb.AppendLine($"[FromQuery(Name = \"{queryName}\")]");
                    else
                        sb.AppendLine("[FromQuery]");
                    break;
                case "httpHeader":
                    if (trait.Properties.TryGetValue("name", out var headerName))
                        sb.AppendLine($"[FromHeader(Name = \"{headerName}\")]");
                    else
                        sb.AppendLine("[FromHeader]");
                    break;
                case "httpPayload":
                    sb.AppendLine("[FromBody]");
                    break;
                default:
                    break;
            }
        }
        return sb.ToString();
    }
}
