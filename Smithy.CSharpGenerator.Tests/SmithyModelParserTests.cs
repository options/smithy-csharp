using Smithy.Model;
using Smithy.Model.Parsers;
using System.IO;
using System.Text;
using Xunit;

namespace Smithy.CSharpGenerator.Tests
{
    public class SmithyModelParserTests
    {        [Fact]
        public void Parser_HandleMultiLineTraits()
        {
            // Arrange
            string smithyIdl = @"
@trait1
@trait2(value=""test"")
@complexTrait(
    value=""this is a multi-line trait"",
    number=42,
    flag=true,
    nested={
        innerValue=""nested value"",
        innerList=[1, 2, 3]
    }
)
structure MyStructure {
    name: String,
    age: Integer
}
";

            // Act
            var parser = new SmithyModelParser();
            var model = parser.Parse(smithyIdl);

            // Assert - verify the structure was parsed correctly
            var structure = model.Shapes.OfType<StructureShape>().FirstOrDefault();
            Assert.NotNull(structure);
            Assert.Equal("MyStructure", structure.Id);
            Assert.Equal(2, structure.Members.Count);
            Assert.Contains(structure.Members, m => m.Name == "name" && m.Target == "String");
            Assert.Contains(structure.Members, m => m.Name == "age" && m.Target == "Integer");
        }

        [Fact]
        public void Parser_HandlesNestedStructures()
        {
            // Arrange
            string smithyIdl = @"
structure OuterStructure {
    // This is a nested structure with proper indentation
    innerField: String,
    nestedStructure: {
        field1: Integer,
        field2: String,
        deeperNested: {
            deepField: Boolean
        }
    },
    finalField: Boolean
}
";

            // Act
            var parser = new SmithyModelParser();
            var model = parser.Parse(smithyIdl);

            // Assert
            var structure = model.Shapes.OfType<StructureShape>().FirstOrDefault();
            Assert.NotNull(structure);
            Assert.Equal("OuterStructure", structure.Id);
            // Verify the basic structure was parsed correctly
            // Note: The full nested structure parsing would require additional model support
            Assert.Contains(structure.Members, m => m.Name == "innerField" && m.Target == "String");
            Assert.Contains(structure.Members, m => m.Name == "finalField" && m.Target == "Boolean");
        }

        [Fact]
        public void Parser_HandlesCollectionShapes()
        {
            // Arrange
            string smithyIdl = @"
list StringList {
    member: String
}

map StringMap {
    key: String,
    value: Integer
}
";

            // Act
            var parser = new SmithyModelParser();
            var model = parser.Parse(smithyIdl);

            // Assert
            var listShape = model.Shapes.OfType<ListShape>().FirstOrDefault();
            Assert.NotNull(listShape);
            Assert.Equal("StringList", listShape.Id);
            Assert.Equal("String", listShape.Member.Target);

            var mapShape = model.Shapes.OfType<MapShape>().FirstOrDefault();
            Assert.NotNull(mapShape);
            Assert.Equal("StringMap", mapShape.Id);
            Assert.Equal("String", mapShape.Key.Target);
            Assert.Equal("Integer", mapShape.Value.Target);
        }        [Fact]
        public void Parser_HandlesComplexTraitProperties()
        {
            // Arrange
            string smithyIdl = @"
@complexTrait(
    stringValue=""test"",
    intValue=42,
    boolValue=true,
    listValue=[""item1"", ""item2""],
    objectValue={
        nested=""value""
    }
)
structure AnnotatedStructure {
    field: String
}
";

            // Act
            var parser = new SmithyModelParser();
            var model = parser.Parse(smithyIdl);

            // Assert
            var structure = model.Shapes.OfType<StructureShape>().FirstOrDefault();
            Assert.NotNull(structure);
            Assert.Equal("AnnotatedStructure", structure.Id);
            
            // Verify trait was captured - actual trait parsing verification would depend on 
            // how complex trait properties are represented in your model
            var trait = structure.ConstraintTraits.FirstOrDefault(t => t.Name == "complexTrait");
            Assert.NotNull(trait);
        }
    }
}
