using System.Collections.Generic;

namespace Smithy.Model
{
    public class StructureShape : Shape
    {
        public List<MemberShape> Members { get; set; } = new();
    }

    public class ServiceShape : Shape
    {
        public List<string> Operations { get; set; } = new();
    }

    public class OperationShape : Shape
    {
        public string? Input { get; set; }
        public string? Output { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class UnionShape : Shape
    {
        public List<MemberShape> Members { get; set; } = new();
    }

    public class EnumShape : Shape
    {
        public List<EnumMemberShape> Members { get; set; } = new();
    }

    public class EnumMemberShape
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Documentation { get; set; }
        public List<ConstraintTrait> ConstraintTraits { get; set; } = new();
    }

    public class IntEnumShape : Shape
    {
        public List<IntEnumMemberShape> Members { get; set; } = new();
    }

    public class IntEnumMemberShape
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string? Documentation { get; set; }
        public List<ConstraintTrait> ConstraintTraits { get; set; } = new();
    }
}
