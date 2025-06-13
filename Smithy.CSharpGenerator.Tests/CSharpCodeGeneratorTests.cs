using Smithy.Model;
using Smithy.CSharpGenerator;
using Xunit;

namespace Smithy.CSharpGenerator.Tests;

public class CSharpCodeGeneratorTests
{
    [Fact]
    public void Generates_Structure_With_Constraints()
    {
        var structure = new StructureShape
        {
            Id = "MyStruct",
            Members = new List<MemberShape>
            {
                new MemberShape
                {
                    Name = "name",
                    Target = "String",
                    ConstraintTraits = new List<ConstraintTrait>
                    {
                        new ConstraintTrait { Name = "minLength", Properties = new Dictionary<string, object> { { "min", 3 } } },
                        new ConstraintTrait { Name = "maxLength", Properties = new Dictionary<string, object> { { "max", 10 } } }
                    }
                },
                new MemberShape
                {
                    Name = "age",
                    Target = "Integer",
                    ConstraintTraits = new List<ConstraintTrait>
                    {
                        new ConstraintTrait { Name = "range", Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 120 } } }
                    }
                }
            }
        };
        var model = new SmithyModel { Shapes = new List<Shape> { structure } };
        var generator = new CSharpCodeGenerator();
        var code = generator.Generate(model);
        Assert.Contains("[MinLength(3)]", code);
        Assert.Contains("[MaxLength(10)]", code);
        Assert.Contains("[Range(0, 120)]", code);
        Assert.Contains("public class MyStruct", code);
        Assert.Contains("public int? Age", code);
    }

    private class StringEnumShape : Shape
    {
        public string Type = "String";
    }

    [Fact]
    public void Generates_Enum_From_EnumTrait()
    {
        var enumTrait = new ConstraintTrait
        {
            Name = "enum",
            Properties = new Dictionary<string, object>
            {
                { "values", new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> { { "name", "FOO" } },
                        new Dictionary<string, object> { { "name", "BAR" } }
                    }
                }
            }
        };
        var shape = new StringEnumShape { Id = "MyEnum", ConstraintTraits = new List<ConstraintTrait> { enumTrait } };
        var model = new SmithyModel { Shapes = new List<Shape> { shape } };
        var generator = new CSharpCodeGenerator();
        var code = generator.Generate(model);
        Assert.Contains("public enum MyEnum", code);
        Assert.Contains("FOO", code);
        Assert.Contains("BAR", code);
    }
}
