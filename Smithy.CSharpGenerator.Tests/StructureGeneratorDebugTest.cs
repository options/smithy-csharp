using Smithy.Model;
using Smithy.CSharpGenerator;
using Smithy.CSharpGenerator.Generators;
using System.Text;
using Xunit;

namespace Smithy.CSharpGenerator.Tests;

public class StructureGeneratorDebugTest
{
    [Fact]
    public void Test_Structure_Generator_For_Integer_Type()
    {
        // Create the same structure as in CSharpCodeGeneratorTests.Generates_Structure_With_Constraints
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
        var structGen = new StructureGenerator();
        var sb = new StringBuilder();
        
        // Directly test the structure generator
        structGen.GenerateStructure(structure, model, sb);
        
        // Output for debugging
        string output = sb.ToString();
        System.Console.WriteLine("GENERATED CODE:");
        System.Console.WriteLine(output);
          // Check the specific value we're looking for
        Assert.Contains("public int? Age { get; set; }", output);
    }
}
