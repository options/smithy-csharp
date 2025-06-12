using System.Collections.Generic;

namespace Smithy.Model
{
    public abstract class Shape
    {
        public string Id { get; set; } = string.Empty;
        public string? Documentation { get; set; }
        public List<ConstraintTrait> ConstraintTraits { get; set; } = new();
    }

    public class MemberShape
    {
        public string Name { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string? Documentation { get; set; }
        public List<ConstraintTrait> ConstraintTraits { get; set; } = new();
    }
}
