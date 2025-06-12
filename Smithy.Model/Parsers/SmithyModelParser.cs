using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Smithy.Model.Parsers
{
    public class SmithyModelParser : ISmithyModelParser
    {
        public SmithyModel Parse(string smithySource)
        {
            var model = new SmithyModel();
            var lines = smithySource.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            List<ConstraintTrait> pendingTraits = new();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                
                // Parse multi-line traits
                while (trimmed.StartsWith("@"))
                {
                    if (trimmed.Contains('(') && !trimmed.TrimEnd().EndsWith(")"))
                    {
                        // Multi-line trait: combine lines until parentheses are closed
                        int open = 0, close = 0;
                        var traitBuilder = new StringBuilder();
                        for (int j = 0; j < trimmed.Length; j++)
                        {
                            if (trimmed[j] == '(') open++;
                            if (trimmed[j] == ')') close++;
                        }
                        traitBuilder.Append(trimmed);
                        while (open > close && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            traitBuilder.Append(' ').Append(nextLine);
                            for (int j = 0; j < nextLine.Length; j++)
                            {
                                if (nextLine[j] == '(') open++;
                                if (nextLine[j] == ')') close++;
                            }
                        }
                        trimmed = traitBuilder.ToString();
                    }
                    int endIdx = trimmed.IndexOf(' ');
                    if (endIdx == -1) break;
                    var traitPart = trimmed.Substring(0, endIdx);
                    var trait = ParseConstraintTrait(traitPart);
                    if (trait != null) pendingTraits.Add(trait);
                    trimmed = trimmed.Substring(endIdx).TrimStart();
                }
                
                // Handle documentation comments (///)
                string? pendingDocumentation = null;
                if (trimmed.StartsWith("///"))
                {
                    var docText = trimmed.Substring(3).Trim();
                    pendingDocumentation = docText;
                    
                    // Look ahead for more documentation lines
                    var docBuilder = new StringBuilder(docText);
                    while (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("///"))
                    {
                        i++;
                        var nextDoc = lines[i].Trim().Substring(3).Trim();
                        docBuilder.AppendLine().Append(nextDoc);
                    }
                    pendingDocumentation = docBuilder.ToString();
                    continue; // Skip to next line
                }
                
                i = ParseShapeDefinition(model, lines, i, trimmed, pendingTraits, pendingDocumentation);
            }
            return model;
        }
        
        // This method will be implemented in a separate partial class file
        private int ParseShapeDefinition(SmithyModel model, string[] lines, int currentIndex, string trimmed, 
            List<ConstraintTrait> pendingTraits, string? pendingDocumentation)
        {
            // Implementation will be moved from Class1.cs
            return currentIndex;
        }
        
        // This method will be implemented in a separate partial class file  
        private ConstraintTrait? ParseConstraintTrait(string traitText)
        {
            // Implementation will be moved from Class1.cs
            return null;
        }
    }
}
