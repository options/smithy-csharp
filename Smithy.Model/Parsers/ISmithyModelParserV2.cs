namespace Smithy.Model.Parsers
{
    /// <summary>
    /// Enhanced Smithy model parser interface with error recovery and diagnostics
    /// </summary>
    public interface ISmithyModelParserV2
    {
        /// <summary>
        /// Parse a Smithy model with error recovery and detailed diagnostics
        /// </summary>
        /// <param name="smithySource">The Smithy IDL source code</param>
        /// <returns>Parse result with model and diagnostics</returns>
        ParseResult ParseWithDiagnostics(string smithySource);
    }
}
