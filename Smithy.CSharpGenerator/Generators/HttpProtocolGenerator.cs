using Smithy.Model;
using Smithy.CSharpGenerator.Utils;
using System.Text;

namespace Smithy.CSharpGenerator.Generators;

public class HttpProtocolGenerator
{
    public string FormatHttpTrait(ConstraintTrait httpTrait)
    {
        if (httpTrait == null || httpTrait.Name != "http") return string.Empty;
        
        var sb = new StringBuilder();
        if (httpTrait.Properties.TryGetValue("method", out var method) && 
            httpTrait.Properties.TryGetValue("uri", out var uri))
        {
            string httpMethod = NamingUtils.PascalCase(method?.ToString() ?? "GET");
            sb.AppendLine($"[Http{httpMethod}(\"{uri}\")]");
        }
        
        if (httpTrait.Properties.TryGetValue("code", out var code))
        {
            sb.AppendLine($"// Response code: {code}");
        }
        
        return sb.ToString();
    }
    
    public string FormatHttpErrorTrait(ConstraintTrait httpErrorTrait)
    {
        if (httpErrorTrait == null || httpErrorTrait.Name != "httpError") return string.Empty;
        
        var sb = new StringBuilder();
        if (httpErrorTrait.Properties.TryGetValue("code", out var code))
        {
            sb.AppendLine($"[ProducesResponseType(typeof(ErrorModel), {code})]");
        }
        
        return sb.ToString();
    }
    
    public string FormatCorsAttributes(IEnumerable<ConstraintTrait> traits)
    {
        var corsTrait = traits?.FirstOrDefault(t => t.Name == "cors");
        if (corsTrait == null) return string.Empty;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[EnableCors]");
        
        if (corsTrait.Properties.TryGetValue("origin", out var origin))
            sb.AppendLine($"// CORS allowed origin: {origin}");
            
        if (corsTrait.Properties.TryGetValue("maxAge", out var maxAge))
            sb.AppendLine($"// CORS max age: {maxAge}");
            
        return sb.ToString();
    }
}
