using System.Linq;
using Xunit;
using Smithy.Model.Parsers;

namespace Smithy.CSharpGenerator.Tests
{
    public class SmithyModelParserV2Tests
    {
        [Fact]
        public void ParseWithDiagnostics_ValidModel_ReturnsSuccess()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example.weather

/// Weather service
service WeatherService {
    version: ""2023-01-01""
}

/// Temperature data
structure Temperature {
    value: Float,
    unit: String
}
";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.HasFatalErrors);
            Assert.Equal("example.weather", result.Model.Namespace);
            Assert.Equal(2, result.Model.Shapes.Count);
        }

        [Fact]
        public void ParseWithDiagnostics_SyntaxError_ReturnsPartialModel()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example.weather

structure Temperature {
    value: Float,
    // Missing closing brace intentionally
";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.HasErrors);
            Assert.NotEmpty(result.Diagnostics);
            
            // Should still parse the namespace
            Assert.Equal("example.weather", result.Model.Namespace);
            
            // Should have error diagnostics
            var errors = result.GetDiagnostics(DiagnosticSeverity.Error);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ParseWithDiagnostics_InvalidTraitSyntax_ReportsErrorWithSuggestion()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example

@required(missing closing paren
structure BadTrait {
    field: String
}
";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.True(result.HasErrors);
            
            var traitErrors = result.Diagnostics
                .Where(d => d.Code == DiagnosticCode.UnbalancedParentheses)
                .ToList();
            
            Assert.NotEmpty(traitErrors);
            Assert.Contains("closing parentheses", traitErrors.First().Message);
            Assert.NotNull(traitErrors.First().SuggestedFix);
        }

        [Fact]
        public void ParseWithDiagnostics_DuplicateShapeNames_ReportsError()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example

structure MyShape {
    field1: String
}

structure MyShape {
    field2: Integer
}
";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.True(result.HasErrors);
            
            var duplicateErrors = result.Diagnostics
                .Where(d => d.Code == DiagnosticCode.DuplicateShapeId)
                .ToList();
            
            Assert.NotEmpty(duplicateErrors);
            Assert.Contains("Duplicate shape ID: MyShape", duplicateErrors.First().Message);
        }

        [Fact]
        public void ParseWithDiagnostics_InvalidMemberSyntax_ReportsErrorWithContext()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example

structure BadMembers {
    validMember: String,
    invalid member syntax here,
    anotherValid: Integer
}
";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.True(result.HasErrors);
            
            var memberErrors = result.Diagnostics
                .Where(d => d.Code == DiagnosticCode.InvalidMemberDefinition)
                .ToList();
            
            Assert.NotEmpty(memberErrors);
            Assert.Contains("memberName: TypeName", memberErrors.First().SuggestedFix);
            Assert.NotNull(memberErrors.First().Context);
        }

        [Fact]
        public void ParseWithDiagnostics_UnknownTraitName_ReportsWarning()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example

@unknownTrait
@invalidTraitName123
structure MyShape {
    field: String
}
";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.True(result.HasWarnings);
            
            var traitWarnings = result.Diagnostics
                .Where(d => d.Code == DiagnosticCode.UnknownTrait && d.Severity == DiagnosticSeverity.Warning)
                .ToList();
            
            Assert.NotEmpty(traitWarnings);
        }

        [Fact]
        public void ParseWithDiagnostics_EmptyDocumentation_ReportsWarning()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example

///
structure MyShape {
    field: String
}
";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.True(result.HasWarnings);
            
            var docWarnings = result.Diagnostics
                .Where(d => d.Code == DiagnosticCode.InvalidDocumentation)
                .ToList();
            
            Assert.NotEmpty(docWarnings);
        }

        [Fact]
        public void ParseWithDiagnostics_LineNumberAccuracy_ReportsCorrectLineNumbers()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"namespace example

structure Valid {
    field: String
}

structure Invalid {
    bad syntax here
}

structure AnotherValid {
    field: Integer
}";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.True(result.HasErrors);
            
            var syntaxError = result.Diagnostics
                .FirstOrDefault(d => d.Code == DiagnosticCode.InvalidMemberDefinition);
            
            Assert.NotNull(syntaxError);
            Assert.Equal(8, syntaxError.LineNumber); // The "bad syntax here" line
        }

        [Fact]
        public void ParseWithDiagnostics_RecoveryAfterError_ContinuesParsingAfterError()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example

structure First {
    field: String
}

structure Bad {
    completely invalid syntax here!!!
}

structure AfterError {
    recoveredField: Integer
}
";

            // Act
            var result = parser.ParseWithDiagnostics(smithyIdl);

            // Assert
            Assert.True(result.HasErrors);
            
            // Should still parse valid shapes before and after the error
            Assert.True(result.Model.Shapes.Count >= 2); // First and AfterError should be parsed
            
            var recoveryInfo = result.Diagnostics
                .Where(d => d.Code == DiagnosticCode.RecoveredFromError || 
                           d.Code == DiagnosticCode.PartialParsingComplete)
                .ToList();
            
            Assert.NotEmpty(recoveryInfo);
        }

        [Fact]
        public void ParseWithDiagnostics_BackwardCompatibility_WorksWithLegacyInterface()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string smithyIdl = @"
namespace example

structure MyShape {
    field: String
}
";

            // Act - Test legacy interface
            var model = parser.Parse(smithyIdl);

            // Assert
            Assert.NotNull(model);
            Assert.Equal("example", model.Namespace);
            Assert.Single(model.Shapes);
        }

        [Fact]
        public void ParseWithDiagnostics_FatalError_ThrowsExceptionInLegacyMode()
        {
            // Arrange
            var parser = new SmithyModelParserV2();
            string invalidSmithyIdl = "completely invalid file content that causes fatal error";

            // Act & Assert
            Assert.Throws<System.FormatException>(() => parser.Parse(invalidSmithyIdl));
        }
    }
}
