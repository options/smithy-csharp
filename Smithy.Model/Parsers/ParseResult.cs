using System.Collections.Generic;

namespace Smithy.Model.Parsers
{
    /// <summary>
    /// Result of parsing a Smithy model, including the model and any diagnostics
    /// </summary>
    public class ParseResult
    {
        public SmithyModel Model { get; set; } = new SmithyModel();
        public List<ParseDiagnostic> Diagnostics { get; set; } = new List<ParseDiagnostic>();
        
        /// <summary>
        /// True if parsing completed without fatal errors
        /// </summary>
        public bool IsSuccess => !HasFatalErrors;
        
        /// <summary>
        /// True if there are any fatal errors
        /// </summary>
        public bool HasFatalErrors => Diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Fatal);
        
        /// <summary>
        /// True if there are any errors (including fatal)
        /// </summary>
        public bool HasErrors => Diagnostics.Exists(d => d.Severity >= DiagnosticSeverity.Error);
        
        /// <summary>
        /// True if there are any warnings
        /// </summary>
        public bool HasWarnings => Diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Warning);
        
        /// <summary>
        /// Get diagnostics of a specific severity level
        /// </summary>
        public IEnumerable<ParseDiagnostic> GetDiagnostics(DiagnosticSeverity severity)
        {
            return Diagnostics.FindAll(d => d.Severity == severity);
        }
        
        /// <summary>
        /// Add a diagnostic to the result
        /// </summary>
        public void AddDiagnostic(ParseDiagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
        
        /// <summary>
        /// Add a diagnostic with basic information
        /// </summary>
        public void AddDiagnostic(DiagnosticSeverity severity, DiagnosticCode code, int lineNumber, string message, string? suggestedFix = null, string? context = null)
        {
            Diagnostics.Add(new ParseDiagnostic
            {
                Severity = severity,
                Code = code,
                LineNumber = lineNumber,
                Message = message,
                SuggestedFix = suggestedFix,
                Context = context
            });
        }
    }
}
