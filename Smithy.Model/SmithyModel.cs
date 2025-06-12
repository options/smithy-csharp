using System.Collections.Generic;

using System.Linq;

namespace Smithy.Model
{
    public class SmithyModel
    {
        public List<Shape> Shapes { get; set; } = new();
        // Smithy IDL extensions
        public string? Namespace { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> Uses { get; set; } = new();
        public List<string> Applies { get; set; } = new();
    }

    public interface ISmithyModelParser
    {
        SmithyModel Parse(string smithySource);
    }
}
