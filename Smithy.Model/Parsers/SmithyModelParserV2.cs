using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Smithy.Model.Parsers
{
    /// <summary>
    /// Enhanced Smithy model parser with error recovery and detailed diagnostics
    /// </summary>
    public class SmithyModelParserV2 : ISmithyModelParserV2, ISmithyModelParser
    {
        /// <summary>
        /// Parse with detailed diagnostics and error recovery
        /// </summary>
        public ParseResult ParseWithDiagnostics(string smithySource)
        {
            var result = new ParseResult();
            var lines = smithySource.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Track parsing state
            var pendingTraits = new List<ConstraintTrait>();
            string? pendingDocumentation = null;
            var lineNumber = 0;
            
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    lineNumber = i + 1; // 1-based line numbers
                    var line = lines[i];
                    var trimmed = line.Trim();
                    
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;
                    
                    try
                    {
                        // Parse traits
                        var traitParseResult = ParseTraitsWithRecovery(lines, ref i, lineNumber, result);
                        pendingTraits.AddRange(traitParseResult.traits);
                        
                        // Update line number after trait parsing
                        lineNumber = i + 1;
                        if (i >= lines.Length) break;
                        
                        line = lines[i];
                        trimmed = line.Trim();
                        
                        // Parse documentation
                        if (trimmed.StartsWith("///"))
                        {
                            var docResult = ParseDocumentationWithRecovery(lines, ref i, lineNumber, result);
                            pendingDocumentation = docResult.documentation;
                            continue;
                        }
                        
                        // Parse shape definition
                        var shapeResult = ParseShapeDefinitionWithRecovery(
                            result.Model, lines, ref i, lineNumber, 
                            pendingTraits, pendingDocumentation, result);
                        
                        // Clear pending items after successful shape parsing
                        if (shapeResult.success)
                        {
                            pendingTraits.Clear();
                            pendingDocumentation = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Non-fatal parsing error - log and continue
                        result.AddDiagnostic(
                            DiagnosticSeverity.Error,
                            DiagnosticCode.UnexpectedToken,
                            lineNumber,
                            $"Unexpected error parsing line: {ex.Message}",
                            "Check syntax and try again",
                            trimmed);
                        
                        // Try to recover by skipping to next meaningful line
                        RecoverToNextShape(lines, ref i, result);
                    }
                }
                
                // Warn about unused pending traits or documentation
                if (pendingTraits.Count > 0)
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Warning,
                        DiagnosticCode.UnexpectedToken,
                        lineNumber,
                        $"Found {pendingTraits.Count} unused trait(s) at end of file",
                        "Ensure traits are applied to shapes");
                }
                
                if (!string.IsNullOrEmpty(pendingDocumentation))
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Warning,
                        DiagnosticCode.InvalidDocumentation,
                        lineNumber,
                        "Found unused documentation comment at end of file",
                        "Ensure documentation is applied to shapes");
                }
                
                result.AddDiagnostic(
                    DiagnosticSeverity.Info,
                    DiagnosticCode.PartialParsingComplete,
                    lineNumber,
                    $"Parsing completed. Found {result.Model.Shapes.Count} shapes with {result.Diagnostics.Count(d => d.Severity >= DiagnosticSeverity.Error)} errors");
            }
            catch (Exception ex)
            {
                // Fatal parsing error
                result.AddDiagnostic(
                    DiagnosticSeverity.Fatal,
                    DiagnosticCode.InvalidSyntax,
                    lineNumber,
                    $"Fatal parsing error: {ex.Message}",
                    "Check file format and syntax");
            }
            
            return result;
        }
        
        /// <summary>
        /// Legacy interface implementation - returns only the model
        /// </summary>
        public SmithyModel Parse(string smithySource)
        {
            var result = ParseWithDiagnostics(smithySource);
            
            // Throw exception for fatal errors to maintain backward compatibility
            if (result.HasFatalErrors)
            {
                var fatalError = result.Diagnostics.First(d => d.Severity == DiagnosticSeverity.Fatal);
                throw new FormatException($"Parsing failed: {fatalError.Message}");
            }
            
            return result.Model;
        }
        
        /// <summary>
        /// Parse traits with error recovery
        /// </summary>
        private (List<ConstraintTrait> traits, bool success) ParseTraitsWithRecovery(
            string[] lines, ref int currentIndex, int lineNumber, ParseResult result)
        {
            var traits = new List<ConstraintTrait>();
            var line = lines[currentIndex];
            var trimmed = line.Trim();
            
            while (trimmed.StartsWith("@"))
            {
                try
                {
                    // Handle multi-line traits
                    if (trimmed.Contains('(') && !trimmed.TrimEnd().EndsWith(")"))
                    {
                        var multiLineResult = ParseMultiLineTraitWithRecovery(lines, ref currentIndex, lineNumber, result);
                        trimmed = multiLineResult.combined;
                        
                        if (!multiLineResult.success)
                        {
                            // Skip this trait and continue
                            break;
                        }
                    }
                    
                    // Parse individual traits from the line
                    var traitResults = ParseTraitLineWithRecovery(trimmed, lineNumber, result);
                    traits.AddRange(traitResults.traits);
                    
                    // Move to next line if we've consumed all traits on this line
                    if (traitResults.remainingText.Trim().Length == 0)
                    {
                        currentIndex++;
                        if (currentIndex >= lines.Length) break;
                        
                        line = lines[currentIndex];
                        trimmed = line.Trim();
                    }
                    else
                    {
                        trimmed = traitResults.remainingText.Trim();
                    }
                }
                catch (Exception ex)
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.InvalidTraitSyntax,
                        lineNumber + (currentIndex - (lineNumber - 1)),
                        $"Error parsing trait: {ex.Message}",
                        "Check trait syntax and parameters",
                        trimmed);
                    
                    // Skip to next line to recover
                    currentIndex++;
                    if (currentIndex >= lines.Length) break;
                    
                    line = lines[currentIndex];
                    trimmed = line.Trim();
                }
            }
            
            return (traits, true);
        }
        
        /// <summary>
        /// Parse multi-line trait with error recovery
        /// </summary>
        private (string combined, bool success) ParseMultiLineTraitWithRecovery(
            string[] lines, ref int currentIndex, int lineNumber, ParseResult result)
        {
            var line = lines[currentIndex];
            var trimmed = line.Trim();
            var traitBuilder = new StringBuilder();
            var indentLevel = line.Length - line.TrimStart().Length;
            
            int open = 0, close = 0;
            const int maxContinuationLines = 50; // Prevent infinite loops
            int continuationCount = 0;
            
            // Count parentheses in first line
            foreach (char c in trimmed)
            {
                if (c == '(') open++;
                if (c == ')') close++;
            }
            traitBuilder.Append(trimmed);
            
            // Continue collecting lines until balanced or max lines reached
            while (open > close && currentIndex + 1 < lines.Length && continuationCount < maxContinuationLines)
            {
                currentIndex++;
                continuationCount++;
                
                var nextLine = lines[currentIndex];
                var nextTrimmed = nextLine.Trim();
                
                if (string.IsNullOrWhiteSpace(nextTrimmed))
                {
                    traitBuilder.AppendLine();
                    continue;
                }
                
                // Check for proper continuation
                int currentIndent = nextLine.Length - nextLine.TrimStart().Length;
                bool isNested = currentIndent > indentLevel;
                
                if (isNested)
                {
                    traitBuilder.AppendLine().Append(nextTrimmed);
                }
                else
                {
                    traitBuilder.Append(' ').Append(nextTrimmed);
                }
                
                // Count parentheses in continuation line
                foreach (char c in nextTrimmed)
                {
                    if (c == '(') open++;
                    if (c == ')') close++;
                }
            }
            
            // Check if we successfully balanced parentheses
            if (open > close)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.UnbalancedParentheses,
                    lineNumber,
                    $"Unbalanced parentheses in trait (expected {open - close} more closing parentheses)",
                    "Add missing closing parentheses or remove extra opening parentheses",
                    traitBuilder.ToString().Substring(0, Math.Min(100, traitBuilder.Length)) + "...");
                
                return (string.Empty, false);
            }
            
            if (continuationCount >= maxContinuationLines)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Warning,
                    DiagnosticCode.InvalidTraitSyntax,
                    lineNumber,
                    $"Trait definition spans too many lines ({continuationCount}+), possible syntax error",
                    "Check for missing closing parentheses or break into multiple traits");
            }
            
            return (traitBuilder.ToString(), true);
        }
        
        /// <summary>
        /// Parse individual traits from a line with recovery
        /// </summary>
        private (List<ConstraintTrait> traits, string remainingText) ParseTraitLineWithRecovery(
            string traitLine, int lineNumber, ParseResult result)
        {
            var traits = new List<ConstraintTrait>();
            var remaining = traitLine;
            
            while (remaining.StartsWith("@"))
            {
                try
                {
                    int endIdx = FindTraitEnd(remaining);
                    if (endIdx <= 0) break;
                    
                    var traitText = remaining.Substring(0, endIdx);
                    var trait = ParseConstraintTraitWithRecovery(traitText, lineNumber, result);
                    
                    if (trait != null)
                    {
                        traits.Add(trait);
                    }
                    
                    remaining = remaining.Substring(endIdx).TrimStart();
                }
                catch (Exception ex)
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.InvalidTraitSyntax,
                        lineNumber,
                        $"Error parsing trait: {ex.Message}",
                        "Check trait syntax",
                        remaining.Substring(0, Math.Min(50, remaining.Length)));
                    
                    // Skip to next whitespace to recover
                    int nextSpace = remaining.IndexOf(' ');
                    if (nextSpace > 0)
                    {
                        remaining = remaining.Substring(nextSpace).TrimStart();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            
            return (traits, remaining);
        }
        
        /// <summary>
        /// Find the end of a trait definition in a line
        /// </summary>
        private int FindTraitEnd(string text)
        {
            if (!text.StartsWith("@")) return 0;
            
            int parenDepth = 0;
            bool inQuote = false;
            
            for (int i = 1; i < text.Length; i++)
            {
                char c = text[i];
                
                if (c == '"' && (i == 0 || text[i - 1] != '\\'))
                {
                    inQuote = !inQuote;
                }
                else if (!inQuote)
                {
                    if (c == '(')
                        parenDepth++;
                    else if (c == ')')
                    {
                        parenDepth--;
                        if (parenDepth < 0) // More closing than opening
                            return i + 1;
                    }
                    else if (c == ' ' && parenDepth == 0)
                    {
                        return i;
                    }
                }
            }
            
            return text.Length;
        }
        
        /// <summary>
        /// Parse constraint trait with error recovery
        /// </summary>
        private ConstraintTrait? ParseConstraintTraitWithRecovery(string traitText, int lineNumber, ParseResult result)
        {
            try
            {
                if (!traitText.StartsWith("@"))
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.InvalidTraitSyntax,
                        lineNumber,
                        "Trait must start with '@'",
                        "Add '@' prefix to trait name",
                        traitText);
                    return null;
                }
                
                var traitName = traitText.Substring(1);
                var properties = new Dictionary<string, object>();
                
                int openParenIndex = traitName.IndexOf('(');
                if (openParenIndex < 0)
                {
                    // Simple trait without parameters
                    return new ConstraintTrait { Name = traitName, Properties = properties };
                }
                
                string name = traitName.Substring(0, openParenIndex);
                
                // Validate trait name
                if (!IsValidTraitName(name))
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Warning,
                        DiagnosticCode.UnknownTrait,
                        lineNumber,
                        $"Unknown or invalid trait name: {name}",
                        "Check trait name spelling and ensure it's a valid Smithy trait",
                        traitText);
                }
                
                // Find matching closing parenthesis
                int closeParenIndex = FindMatchingCloseParen(traitName, openParenIndex);
                if (closeParenIndex < 0)
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.UnbalancedParentheses,
                        lineNumber,
                        "Missing closing parenthesis in trait",
                        "Add closing parenthesis ')' to complete trait definition",
                        traitText);
                    return null;
                }
                
                // Parse trait parameters
                string paramContent = traitName.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
                properties = ParseTraitPropertiesWithRecovery(paramContent, lineNumber, result);
                
                return new ConstraintTrait { Name = name, Properties = properties };
            }
            catch (Exception ex)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidTraitSyntax,
                    lineNumber,
                    $"Failed to parse trait: {ex.Message}",
                    "Check trait syntax and parameters",
                    traitText);
                return null;
            }
        }
        
        /// <summary>
        /// Check if a trait name is valid
        /// </summary>
        private bool IsValidTraitName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            
            // Basic validation - starts with letter, contains only letters, numbers, dots
            return Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9\.]*$");
        }
        
        /// <summary>
        /// Find matching closing parenthesis
        /// </summary>
        private int FindMatchingCloseParen(string text, int openIndex)
        {
            int depth = 0;
            bool inQuote = false;
            
            for (int i = openIndex; i < text.Length; i++)
            {
                char c = text[i];
                
                if (c == '"' && (i == 0 || text[i - 1] != '\\'))
                {
                    inQuote = !inQuote;
                }
                else if (!inQuote)
                {
                    if (c == '(')
                        depth++;
                    else if (c == ')')
                    {
                        depth--;
                        if (depth == 0)
                            return i;
                    }
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Parse trait properties with error recovery
        /// </summary>
        private Dictionary<string, object> ParseTraitPropertiesWithRecovery(string content, int lineNumber, ParseResult result)
        {
            var properties = new Dictionary<string, object>();
            
            if (string.IsNullOrWhiteSpace(content))
                return properties;
            
            try
            {
                // Handle simple value lists without property names
                if (!content.Contains(':') && !content.Contains('='))
                {
                    var values = SplitRespectingQuotes(content, ',');
                    for (int i = 0; i < values.Length; i++)
                    {
                        string cleanValue = CleanQuotedString(values[i].Trim());
                        if (!string.IsNullOrEmpty(cleanValue))
                        {
                            properties[$"value{i}"] = cleanValue;
                        }
                    }
                    return properties;
                }
                
                // Parse property:value pairs
                var pairs = SplitRespectingQuotes(content, ',');
                foreach (var pair in pairs)
                {
                    var trimmedPair = pair.Trim();
                    if (string.IsNullOrEmpty(trimmedPair)) continue;
                    
                    var separatorIndex = FindPropertySeparator(trimmedPair);
                    if (separatorIndex > 0)
                    {
                        string propertyName = trimmedPair.Substring(0, separatorIndex).Trim();
                        string propertyValue = trimmedPair.Substring(separatorIndex + 1).Trim();
                        
                        if (!string.IsNullOrEmpty(propertyName))
                        {
                            properties[CleanQuotedString(propertyName)] = ParseTraitValue(propertyValue);
                        }
                    }
                    else
                    {
                        result.AddDiagnostic(
                            DiagnosticSeverity.Warning,
                            DiagnosticCode.InvalidTraitParameter,
                            lineNumber,
                            $"Could not parse trait parameter: {trimmedPair}",
                            "Use format 'name: value' or 'name = value'",
                            trimmedPair);
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidTraitParameter,
                    lineNumber,
                    $"Error parsing trait parameters: {ex.Message}",
                    "Check parameter syntax",
                    content);
            }
            
            return properties;
        }
        
        /// <summary>
        /// Find property separator (: or =)
        /// </summary>
        private int FindPropertySeparator(string text)
        {
            bool inQuote = false;
            int braceDepth = 0;
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                if (c == '"' && (i == 0 || text[i - 1] != '\\'))
                {
                    inQuote = !inQuote;
                }
                else if (!inQuote)
                {
                    if (c == '{') braceDepth++;
                    else if (c == '}') braceDepth--;
                    else if ((c == ':' || c == '=') && braceDepth == 0)
                    {
                        return i;
                    }
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Parse documentation with error recovery
        /// </summary>
        private (string? documentation, bool success) ParseDocumentationWithRecovery(
            string[] lines, ref int currentIndex, int lineNumber, ParseResult result)
        {
            var line = lines[currentIndex];
            var trimmed = line.Trim();
            
            if (!trimmed.StartsWith("///"))
                return (null, false);
            
            var docBuilder = new StringBuilder();
            var firstDocText = trimmed.Substring(3).Trim();
            docBuilder.Append(firstDocText);
            
            // Look ahead for continuation documentation lines
            while (currentIndex + 1 < lines.Length)
            {
                var nextLine = lines[currentIndex + 1];
                var nextTrimmed = nextLine.Trim();
                
                if (nextTrimmed.StartsWith("///"))
                {
                    currentIndex++;
                    var nextDocText = nextTrimmed.Substring(3).Trim();
                    docBuilder.AppendLine().Append(nextDocText);
                }
                else
                {
                    break;
                }
            }
            
            var documentation = docBuilder.ToString();
            
            // Validate documentation content
            if (string.IsNullOrWhiteSpace(documentation))
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Warning,
                    DiagnosticCode.InvalidDocumentation,
                    lineNumber,
                    "Empty documentation comment found",
                    "Add meaningful documentation or remove empty comment");
                return (null, true);
            }
            
            return (documentation, true);
        }
          /// <summary>
        /// Parse shape definition with error recovery
        /// </summary>
        private (bool success, string message) ParseShapeDefinitionWithRecovery(
            SmithyModel model, string[] lines, ref int currentIndex, int lineNumber,
            List<ConstraintTrait> pendingTraits, string? pendingDocumentation, ParseResult result)
        {
            var line = lines[currentIndex];
            var trimmed = line.Trim();
            
            try
            {
                // Check for namespace, use, apply directives
                var directiveMatch = Regex.Match(trimmed, @"^(namespace|use|apply)\s+(.+)$");
                if (directiveMatch.Success)
                {
                    var directive = directiveMatch.Groups[1].Value;
                    var value = directiveMatch.Groups[2].Value.TrimEnd(';');
                    
                    switch (directive)
                    {                        case "namespace":
                            if (!string.IsNullOrEmpty(model.Namespace))
                            {
                                result.AddDiagnostic(
                                    DiagnosticSeverity.Warning,
                                    DiagnosticCode.DuplicateShapeId,
                                    lineNumber,
                                    "Multiple namespace declarations found",
                                    "Use only one namespace per file",
                                    trimmed);
                            }
                            model.Namespace = value;
                            break;
                        case "use":
                            model.Uses.Add(value);
                            break;
                        case "apply":
                            model.Applies.Add(value);
                            break;
                    }
                    return (true, "Directive parsed successfully");
                }
                
                // Parse shape definition
                var shapeMatch = Regex.Match(trimmed, @"^([\w\.]+)\s+([A-Za-z0-9_#\.]+)\s*(\{)?");                if (!shapeMatch.Success)
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.InvalidSyntax,
                        lineNumber,
                        "Could not parse shape definition",
                        "Use format: 'shapeType ShapeName { ... }'",
                        trimmed);
                    return (false, "Invalid shape syntax");
                }
                
                string shapeType = shapeMatch.Groups[1].Value;
                string shapeId = shapeMatch.Groups[2].Value;
                bool hasBrace = shapeMatch.Groups[3].Success;
                  // Validate shape ID
                if (!IsValidShapeId(shapeId))
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.MissingShapeId,
                        lineNumber,
                        $"Invalid shape ID: {shapeId}",
                        "Shape IDs must start with a letter and contain only letters, numbers, and underscores",
                        trimmed);
                    return (false, "Invalid shape ID");
                }
                
                // Check for duplicate shape IDs
                if (model.Shapes.Any(s => s.Id == shapeId))
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.DuplicateShapeId,
                        lineNumber,
                        $"Duplicate shape ID: {shapeId}",
                        "Use unique shape IDs within the model",
                        trimmed);
                    return (false, "Duplicate shape ID");
                }
                
                // Create shape based on type
                Shape? shape = CreateShapeFromType(shapeType, shapeId, lineNumber, result);
                if (shape == null)
                {
                    return (false, "Failed to create shape");
                }
                
                // Apply pending traits and documentation
                shape.ConstraintTraits.AddRange(pendingTraits);
                shape.Documentation = pendingDocumentation;
                
                // Parse shape body if present
                if (hasBrace)
                {
                    var bodyResult = ParseShapeBodyWithRecovery(shape, lines, ref currentIndex, lineNumber, result);
                    if (!bodyResult.success)
                    {
                        result.AddDiagnostic(
                            DiagnosticSeverity.Warning,
                            DiagnosticCode.RecoveredFromError,
                            lineNumber,
                            $"Partial parsing of shape '{shapeId}' - some members may be missing",
                            "Check shape body syntax");
                    }
                }
                  model.Shapes.Add(shape);
                return (true, "Shape parsed successfully");
            }
            catch (Exception ex)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidShapeStructure,
                    lineNumber,
                    $"Error parsing shape: {ex.Message}",
                    "Check shape definition syntax",
                    trimmed);
                return (false, "Exception during shape parsing");
            }
        }
        
        /// <summary>
        /// Check if shape ID is valid
        /// </summary>
        private bool IsValidShapeId(string shapeId)
        {
            if (string.IsNullOrWhiteSpace(shapeId)) return false;
            return Regex.IsMatch(shapeId, @"^[a-zA-Z][a-zA-Z0-9_#\.]*$");
        }
        
        /// <summary>
        /// Create shape from type string
        /// </summary>
        private Shape? CreateShapeFromType(string shapeType, string shapeId, int lineNumber, ParseResult result)
        {
            try
            {
                return shapeType.ToLower() switch
                {
                    "structure" => new StructureShape { Id = shapeId },
                    "list" => new ListShape { Id = shapeId },
                    "map" => new MapShape { Id = shapeId },
                    "service" => new ServiceShape { Id = shapeId },
                    "operation" => new OperationShape { Id = shapeId },
                    "resource" => new ResourceShape { Id = shapeId },
                    "string" => new StringShape(shapeId),
                    "integer" => new IntegerShape(shapeId),
                    "boolean" => new BooleanShape(shapeId),
                    "float" => new FloatShape(shapeId),
                    "double" => new DoubleShape(shapeId),
                    "long" => new LongShape(shapeId),
                    "short" => new ShortShape(shapeId),
                    "byte" => new ByteShape(shapeId),
                    "blob" => new BlobShape(shapeId),
                    "timestamp" => new TimestampShape(shapeId),
                    "document" => new DocumentShape(shapeId),
                    "union" => new UnionShape { Id = shapeId },
                    "enum" => new StringShape(shapeId), // Enums are string shapes with enum trait
                    _ => null
                };
            }
            catch
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidShapeType,
                    lineNumber,
                    $"Unknown shape type: {shapeType}",
                    "Use a valid Smithy shape type (structure, list, map, service, operation, resource, string, etc.)",
                    shapeType);
                return null;
            }
        }
          /// <summary>
        /// Parse shape body with error recovery
        /// </summary>
        private (bool success, string message) ParseShapeBodyWithRecovery(
            Shape shape, string[] lines, ref int currentIndex, int lineNumber, ParseResult result)
        {
            int braceCount = 1; // We've already seen the opening brace
            int bodyStartLine = lineNumber;
            int memberCount = 0;
            
            try
            {
                currentIndex++; // Move past the opening brace line
                
                while (currentIndex < lines.Length && braceCount > 0)
                {
                    var line = lines[currentIndex];
                    var trimmed = line.Trim();
                    
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        currentIndex++;
                        continue;
                    }
                    
                    // Count braces
                    foreach (char c in trimmed)
                    {
                        if (c == '{') braceCount++;
                        else if (c == '}') braceCount--;
                    }
                    
                    // Process member line (skip closing brace)
                    if (braceCount > 0)
                    {
                        var memberResult = ProcessShapeMemberWithRecovery(shape, trimmed, currentIndex + 1, result);
                        if (memberResult.success)
                        {
                            memberCount++;
                        }
                    }
                    
                    currentIndex++;
                }
                  // Validate that we found the closing brace
                if (braceCount > 0)
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.MissingToken,
                        currentIndex,
                        $"Missing closing brace for shape starting at line {bodyStartLine}",
                        "Add '}' to close the shape definition");
                    return (false, "Missing closing brace");
                }
                
                // Validate that we found some content for aggregate shapes
                if (shape is StructureShape || shape is UnionShape)
                {
                    if (memberCount == 0)
                    {
                        result.AddDiagnostic(
                            DiagnosticSeverity.Warning,
                            DiagnosticCode.InvalidShapeStructure,
                            bodyStartLine,
                            $"Empty {shape.GetType().Name.Replace("Shape", "").ToLower()} definition",
                            "Add members to the shape or remove empty braces");
                    }
                }
                
                return (true, "Shape body parsed successfully");
            }
            catch (Exception ex)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidShapeStructure,
                    lineNumber,
                    $"Error parsing shape body: {ex.Message}",
                    "Check shape member syntax");
                return (false, "Exception during shape body parsing");
            }
        }
        
        /// <summary>
        /// Process shape member with error recovery
        /// </summary>
        private (bool success, string message) ProcessShapeMemberWithRecovery(Shape shape, string line, int lineNumber, ParseResult result)
        {
            try
            {
                if (shape is StructureShape structShape)
                {
                    return ProcessStructureMemberWithRecovery(structShape, line, lineNumber, result);
                }
                else if (shape is ListShape listShape)
                {
                    return ProcessListMemberWithRecovery(listShape, line, lineNumber, result);
                }
                else if (shape is MapShape mapShape)
                {
                    return ProcessMapMemberWithRecovery(mapShape, line, lineNumber, result);
                }
                else if (shape is ServiceShape serviceShape)
                {
                    return ProcessServiceMemberWithRecovery(serviceShape, line, lineNumber, result);
                }
                else if (shape is OperationShape operationShape)
                {
                    return ProcessOperationMemberWithRecovery(operationShape, line, lineNumber, result);
                }
                  // Unknown shape type for member processing
                result.AddDiagnostic(
                    DiagnosticSeverity.Warning,
                    DiagnosticCode.InvalidShapeStructure,
                    lineNumber,
                    $"Cannot process members for shape type: {shape.GetType().Name}",
                    "Check if this shape type should have members");
                
                return (false, "Unknown shape type");
            }
            catch (Exception ex)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidMemberDefinition,
                    lineNumber,
                    $"Error processing shape member: {ex.Message}",
                    "Check member syntax",
                    line);
                return (false, "Exception during member processing");
            }
        }
        
        /// <summary>
        /// Process structure member with error recovery
        /// </summary>
        private (bool success, string message) ProcessStructureMemberWithRecovery(StructureShape structShape, string line, int lineNumber, ParseResult result)
        {            var memberMatch = Regex.Match(line, @"^([A-Za-z0-9_]+)\s*:\s*([A-Za-z0-9_#\.]+)");
            if (!memberMatch.Success)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidMemberDefinition,
                    lineNumber,
                    "Invalid structure member definition",
                    "Use format: 'memberName: TypeName'",
                    line);
                return (false, "Invalid member syntax");
            }
            
            string memberName = memberMatch.Groups[1].Value;
            string targetShape = memberMatch.Groups[2].Value;
            
            // Validate member name
            if (!IsValidMemberName(memberName))
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidMemberName,
                    lineNumber,
                    $"Invalid member name: {memberName}",
                    "Member names must start with a letter and contain only letters, numbers, and underscores",
                    line);
                return (false, "Invalid member name");
            }
            
            // Check for duplicate member names
            if (structShape.Members.Any(m => m.Name == memberName))
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.DuplicateShapeId,
                    lineNumber,
                    $"Duplicate member name: {memberName}",
                    "Use unique member names within the structure",
                    line);
                return (false, "Duplicate member name");
            }
            
            structShape.Members.Add(new MemberShape
            {
                Name = memberName,
                Target = targetShape
            });
            
            return (true, "Structure member processed successfully");
        }
          /// <summary>
        /// Process list member with error recovery
        /// </summary>
        private (bool success, string message) ProcessListMemberWithRecovery(ListShape listShape, string line, int lineNumber, ParseResult result)
        {            var memberMatch = Regex.Match(line, @"^member\s*:\s*([A-Za-z0-9_#\.]+)");
            if (!memberMatch.Success)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.InvalidMemberDefinition,
                    lineNumber,
                    "Invalid list member definition",
                    "Use format: 'member: TypeName'",
                    line);
                return (false, "Invalid list member syntax");
            }
            
            string targetShape = memberMatch.Groups[1].Value;
            
            if (listShape.Member != null)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Warning,
                    DiagnosticCode.DuplicateShapeId,
                    lineNumber,
                    "Multiple member definitions in list",
                    "Lists should have only one member definition");
            }
            
            listShape.Member = new MemberShape
            {
                Name = "member",
                Target = targetShape
            };
            
            return (true, "List member processed successfully");
        }
          /// <summary>
        /// Process map member with error recovery
        /// </summary>
        private (bool success, string message) ProcessMapMemberWithRecovery(MapShape mapShape, string line, int lineNumber, ParseResult result)
        {
            var keyMatch = Regex.Match(line, @"^key\s*:\s*([A-Za-z0-9_#\.]+)");
            if (keyMatch.Success)
            {
                string targetShape = keyMatch.Groups[1].Value;
                
                if (mapShape.Key != null)
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Warning,
                        DiagnosticCode.DuplicateShapeId,
                        lineNumber,
                        "Multiple key definitions in map",
                        "Maps should have only one key definition");
                }
                  mapShape.Key = new MemberShape
                {
                    Name = "key",
                    Target = targetShape
                };
                return (true, "Map key processed successfully");
            }
            
            var valueMatch = Regex.Match(line, @"^value\s*:\s*([A-Za-z0-9_#\.]+)");
            if (valueMatch.Success)
            {
                string targetShape = valueMatch.Groups[1].Value;
                
                if (mapShape.Value != null)
                {
                    result.AddDiagnostic(
                        DiagnosticSeverity.Warning,
                        DiagnosticCode.DuplicateShapeId,
                        lineNumber,
                        "Multiple value definitions in map",
                        "Maps should have only one value definition");
                }
                
                mapShape.Value = new MemberShape
                {
                    Name = "value",
                    Target = targetShape
                };
                return (true, "Map value processed successfully");
            }
            
            result.AddDiagnostic(
                DiagnosticSeverity.Error,
                DiagnosticCode.InvalidMemberDefinition,
                lineNumber,
                "Invalid map member definition",
                "Use format: 'key: TypeName' or 'value: TypeName'",
                line);
            return (false, "Invalid map member syntax");
        }
          /// <summary>
        /// Process service member with error recovery
        /// </summary>
        private (bool success, string message) ProcessServiceMemberWithRecovery(ServiceShape serviceShape, string line, int lineNumber, ParseResult result)
        {
            // Simple implementation for now - just log that we found a service member
            result.AddDiagnostic(
                DiagnosticSeverity.Info,
                DiagnosticCode.PartialParsingComplete,
                lineNumber,
                "Service member parsing not fully implemented",
                "Service member parsing will be enhanced in future versions",
                line);
            return (true, "Service member logged");
        }
        
        /// <summary>
        /// Process operation member with error recovery
        /// </summary>
        private (bool success, string message) ProcessOperationMemberWithRecovery(OperationShape operationShape, string line, int lineNumber, ParseResult result)
        {
            // Simple implementation for now - just log that we found an operation member
            result.AddDiagnostic(
                DiagnosticSeverity.Info,
                DiagnosticCode.PartialParsingComplete,
                lineNumber,
                "Operation member parsing not fully implemented",
                "Operation member parsing will be enhanced in future versions",
                line);
            return (true, "Operation member logged");
        }
        
        /// <summary>
        /// Check if member name is valid
        /// </summary>
        private bool IsValidMemberName(string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName)) return false;
            return Regex.IsMatch(memberName, @"^[a-zA-Z][a-zA-Z0-9_]*$");
        }
        
        /// <summary>
        /// Recover to next shape definition
        /// </summary>
        private void RecoverToNextShape(string[] lines, ref int currentIndex, ParseResult result)
        {
            var originalIndex = currentIndex;
            
            // Look for next shape definition or EOF
            while (currentIndex < lines.Length)
            {
                var line = lines[currentIndex].Trim();
                
                // Check if this looks like a shape definition
                if (Regex.IsMatch(line, @"^(structure|list|map|service|operation|resource|string|integer|boolean|float|double|long|short|byte|blob|timestamp|document|union|enum)\s+[A-Za-z0-9_#\.]+"))
                {
                    // Found next shape, back up one so the main loop will process it
                    currentIndex--;
                    break;
                }
                
                currentIndex++;
            }
            
            var skippedLines = currentIndex - originalIndex;
            if (skippedLines > 0)
            {
                result.AddDiagnostic(
                    DiagnosticSeverity.Info,
                    DiagnosticCode.RecoveredFromError,
                    originalIndex + 1,
                    $"Skipped {skippedLines} lines to recover from parsing error");
            }
        }
        
        // Helper methods from original parser
        private object ParseTraitValue(string value)
        {
            value = value.Trim();
            
            if ((value.StartsWith("\"") && value.EndsWith("\"")) || 
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                return CleanQuotedString(value);
            }
            
            if (bool.TryParse(value, out bool boolValue))
            {
                return boolValue;
            }
            
            if (double.TryParse(value, out double numValue))
            {
                if (numValue % 1 == 0 && numValue <= int.MaxValue && numValue >= int.MinValue)
                {
                    return (int)numValue;
                }
                return numValue;
            }
            
            return value;
        }
        
        private string[] SplitRespectingQuotes(string input, char separator)
        {
            var result = new List<string>();
            bool inQuotes = false;
            int startPos = 0;
            
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '"' && (i == 0 || input[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (input[i] == separator && !inQuotes)
                {
                    result.Add(input.Substring(startPos, i - startPos));
                    startPos = i + 1;
                }
            }
            
            result.Add(input.Substring(startPos));
            return result.ToArray();
        }
        
        private string CleanQuotedString(string value)
        {
            value = value.Trim();
            if ((value.StartsWith("\"") && value.EndsWith("\"")) || 
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                return value.Substring(1, value.Length - 2)
                    .Replace("\\\"", "\"")
                    .Replace("\\'", "'");
            }
            return value;
        }
    }
    
    // Additional shape classes for completeness
    public class StringShape : Shape { public StringShape(string id) { Id = id; } }
    public class IntegerShape : Shape { public IntegerShape(string id) { Id = id; } }
    public class BooleanShape : Shape { public BooleanShape(string id) { Id = id; } }
    public class FloatShape : Shape { public FloatShape(string id) { Id = id; } }
    public class DoubleShape : Shape { public DoubleShape(string id) { Id = id; } }
    public class LongShape : Shape { public LongShape(string id) { Id = id; } }
    public class ShortShape : Shape { public ShortShape(string id) { Id = id; } }
    public class ByteShape : Shape { public ByteShape(string id) { Id = id; } }
    public class BlobShape : Shape { public BlobShape(string id) { Id = id; } }
    public class TimestampShape : Shape { public TimestampShape(string id) { Id = id; } }
    public class DocumentShape : Shape { public DocumentShape(string id) { Id = id; } }
}
