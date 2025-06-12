using System.Collections.Generic;
using System.Linq;

namespace Smithy.Model
{
    public class ConstraintTrait
    {
        public string Name { get; set; } = string.Empty; // e.g., "length", "pattern"
        public Dictionary<string, object> Properties { get; set; } = new(); // e.g., {"min": 1, "max": 10}
        
        public override string ToString()
        {
            if (Properties.Count == 0) return $"@{Name}";
            var props = string.Join(", ", Properties.Select(kv => $"{kv.Key}={kv.Value}"));
            return $"@{Name}({props})";
        }
    }
}
