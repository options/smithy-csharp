using System.Collections.Generic;

namespace Smithy.Model
{
    public class ListShape : Shape
    {
        public MemberShape Member { get; set; } = new() { Name = "member", Target = "String" };
    }

    public class SetShape : Shape
    {
        public MemberShape Member { get; set; } = new() { Name = "member", Target = "String" };
    }

    public class MapShape : Shape
    {
        public MemberShape Key { get; set; } = new() { Name = "key", Target = "String" };
        public MemberShape Value { get; set; } = new() { Name = "value", Target = "String" };
    }

    public class ResourceShape : Shape
    {
        public Dictionary<string, string> Identifiers { get; set; } = new();
        public Dictionary<string, string> Properties { get; set; } = new();
        public string? Create { get; set; }
        public string? Read { get; set; }
        public string? Update { get; set; }
        public string? Delete { get; set; }
        public string? List { get; set; }
        public string? Put { get; set; }
        public List<string> Operations { get; set; } = new();
        public List<string> CollectionOperations { get; set; } = new();
        public List<string> Resources { get; set; } = new();
    }
}
