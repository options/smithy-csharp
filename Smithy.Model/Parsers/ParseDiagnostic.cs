using System;

namespace Smithy.Model.Parsers
{
    /// <summary>
    /// Represents a diagnostic message from the Smithy parser
    /// </summary>
    public class ParseDiagnostic
    {
        public DiagnosticSeverity Severity { get; set; }
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? SuggestedFix { get; set; }
        public string? Context { get; set; }
        public DiagnosticCode Code { get; set; }

        public override string ToString()
        {
            var severityText = Severity switch
            {
                DiagnosticSeverity.Error => "Error",
                DiagnosticSeverity.Warning => "Warning",
                DiagnosticSeverity.Info => "Info",
                _ => "Unknown"
            };

            var location = $"Line {LineNumber}";
            if (ColumnNumber > 0)
                location += $", Column {ColumnNumber}";

            var result = $"{severityText} [{Code}] at {location}: {Message}";
            
            if (!string.IsNullOrEmpty(Context))
                result += $"\n  Context: {Context}";
            
            if (!string.IsNullOrEmpty(SuggestedFix))
                result += $"\n  Suggested fix: {SuggestedFix}";

            return result;
        }
    }

    /// <summary>
    /// Severity levels for parser diagnostics
    /// </summary>
    public enum DiagnosticSeverity
    {
        /// <summary>
        /// Information message - does not affect parsing
        /// </summary>
        Info,
        /// <summary>
        /// Warning - parsing can continue but output may be incomplete
        /// </summary>
        Warning,
        /// <summary>
        /// Error - parsing can continue with recovery, but shape may be incomplete
        /// </summary>
        Error,
        /// <summary>
        /// Fatal error - parsing cannot continue
        /// </summary>
        Fatal
    }

    /// <summary>
    /// Diagnostic codes for different types of parser issues
    /// </summary>
    public enum DiagnosticCode
    {
        // General parsing errors
        UnexpectedToken = 1000,
        MissingToken = 1001,
        InvalidSyntax = 1002,
        
        // Shape definition errors
        InvalidShapeType = 2000,
        MissingShapeId = 2001,
        DuplicateShapeId = 2002,
        InvalidShapeStructure = 2003,
        
        // Trait errors
        InvalidTraitSyntax = 3000,
        UnknownTrait = 3001,
        MissingTraitParameter = 3002,
        InvalidTraitParameter = 3003,
        UnbalancedParentheses = 3004,
        
        // Member errors
        InvalidMemberDefinition = 4000,
        MissingMemberType = 4001,
        InvalidMemberName = 4002,
        
        // Type errors
        UnknownType = 5000,
        InvalidTypeReference = 5001,
        
        // Documentation errors
        InvalidDocumentation = 6000,
        
        // Recovery messages
        RecoveredFromError = 9000,
        PartialParsingComplete = 9001
    }
}
