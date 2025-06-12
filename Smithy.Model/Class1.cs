using System;
using System.Collections.Generic;
using System.Linq;

namespace Smithy.Model
{

    public class ConstraintTrait
    {
        public string Name { get; set; } = string.Empty; // e.g., "length", "pattern"
        public Dictionary<string, object> Properties { get; set; } = new(); // e.g., {"min": 1, "max": 10}
        public override string ToString()
        {
            if (Properties.Count == 0) return $"@{Name}";
            var props = string.Join(", ", Properties.Select(kv => $"{kv.Key}={kv.Value}"));
            return $"@{Name}({props})";
        }
    }

    public abstract class Shape
    {
        public string Id { get; set; } = string.Empty;
        public string? Documentation { get; set; }
        public List<ConstraintTrait> ConstraintTraits { get; set; } = new();
    }

    public class MemberShape
    {
        public string Name { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string? Documentation { get; set; }
        public List<ConstraintTrait> ConstraintTraits { get; set; } = new();
    }

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

    public class SmithyModel
    {
        public List<Shape> Shapes { get; set; } = new();
        // Smithy IDL extensions
        public string? Namespace { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> Uses { get; set; } = new();
        public List<string> Applies { get; set; } = new();
    }

    public interface ISmithyModelParser
    {
        SmithyModel Parse(string smithySource);
    }

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
                // 여러 줄 trait 파싱 지원
                while (trimmed.StartsWith("@"))
                {
                    if (trimmed.Contains('(') && !trimmed.TrimEnd().EndsWith(")"))
                    {
                        // 여러 줄 trait: 괄호 쌍이 닫힐 때까지 다음 줄을 합침
                        int open = 0, close = 0;
                        var traitBuilder = new System.Text.StringBuilder();
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
                    var traitPart = trimmed.Substring(0, endIdx);                    var trait = ParseConstraintTrait(traitPart);
                    if (trait != null) pendingTraits.Add(trait);
                    trimmed = trimmed.Substring(endIdx).TrimStart();                }
                
                // Handle documentation comments (///)
                string? pendingDocumentation = null;
                if (trimmed.StartsWith("///"))
                {
                    var docText = trimmed.Substring(3).Trim();
                    pendingDocumentation = docText;
                    
                    // Look ahead for more documentation lines
                    var docBuilder = new System.Text.StringBuilder(docText);
                    while (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("///"))
                    {
                        i++;
                        var nextDoc = lines[i].Trim().Substring(3).Trim();
                        docBuilder.AppendLine().Append(nextDoc);
                    }
                    pendingDocumentation = docBuilder.ToString();
                    continue; // Skip to next line
                }
                
                // namespace 파싱
                if (trimmed.StartsWith("namespace "))
                {
                    var namespaceParts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (namespaceParts.Length >= 2)
                    {
                        model.Namespace = namespaceParts[1];
                    }
                }                else if (trimmed.StartsWith("service "))
                {
                    // service ExampleService { version: "1.0" operations: [ExampleOperation] }
                    var id = trimmed.Split(' ')[1];
                    var svc = new ServiceShape { Id = id };
                    if (pendingDocumentation != null)
                    {
                        svc.Documentation = pendingDocumentation;
                        pendingDocumentation = null;
                    }
                    svc.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // 여러 줄에 걸친 service 정의 파싱
                    var serviceContent = new System.Text.StringBuilder();
                    serviceContent.Append(trimmed);
                    
                    // service 블록이 여러 줄인 경우 전체 내용을 수집
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            serviceContent.Append(' ').Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullServiceText = serviceContent.ToString();
                    var opsIdx = fullServiceText.IndexOf("operations:");
                    if (opsIdx >= 0)
                    {
                        var start = fullServiceText.IndexOf('[', opsIdx);
                        var end = fullServiceText.IndexOf(']', opsIdx);
                        if (start >= 0 && end > start)
                        {
                            var opsList = fullServiceText.Substring(start + 1, end - start - 1)
                                .Split(new[] { ',', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            svc.Operations.AddRange(opsList);
                        }
                    }
                    model.Shapes.Add(svc);
                }
                else if (trimmed.StartsWith("operation "))
                {
                    var id = trimmed.Split(' ')[1];
                    var op = new OperationShape { Id = id };
                    op.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Handle multi-line operation definitions
                    var operationContent = new System.Text.StringBuilder();
                    operationContent.Append(trimmed);
                    
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            operationContent.AppendLine().Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullOperationText = operationContent.ToString();
                    
                    // Parse inline input definition (input := { ... })
                    var inputInlineMatch = System.Text.RegularExpressions.Regex.Match(fullOperationText, @"input\s*:=\s*\{([^}]*)\}");
                    if (inputInlineMatch.Success)
                    {
                        var inputStructureId = $"{id}Input";
                        var inputStructure = ParseInlineStructure(inputStructureId, inputInlineMatch.Groups[1].Value);
                        model.Shapes.Add(inputStructure);
                        op.Input = inputStructureId;
                    }
                    else
                    {
                        // Legacy input parsing
                        var inputIdx = fullOperationText.IndexOf("input:");
                        if (inputIdx >= 0)
                        {
                            var afterInput = fullOperationText.Substring(inputIdx + 6).TrimStart();
                            var inputVal = afterInput.Split(' ', '}')[0].Trim();
                            op.Input = inputVal;
                        }
                    }
                      // Parse inline output definition (output := { ... })
                    var outputInlineMatch = System.Text.RegularExpressions.Regex.Match(fullOperationText, @"output\s*:=\s*\{([^}]*)\}");
                    if (outputInlineMatch.Success)
                    {
                        var outputStructureId = $"{id}Output";
                        var outputStructure = ParseInlineStructure(outputStructureId, outputInlineMatch.Groups[1].Value);
                        model.Shapes.Add(outputStructure);
                        op.Output = outputStructureId;
                    }
                    else
                    {
                        // Parse "for Resource" syntax (output := for ResourceName { ... })
                        var forResourceMatch = System.Text.RegularExpressions.Regex.Match(fullOperationText, @"output\s*:=\s*for\s+(\w+)\s*\{([^}]*)\}");
                        if (forResourceMatch.Success)
                        {
                            var resourceName = forResourceMatch.Groups[1].Value;
                            var memberList = forResourceMatch.Groups[2].Value;
                            var outputStructureId = $"{id}Output";
                            
                            // Create structure with members from resource
                            var outputStructure = new StructureShape { Id = outputStructureId };
                            
                            // Parse members (e.g., $forecastId, $temperature, etc.)
                            var members = memberList.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var memberLine in members)
                            {
                                var memberTrimmed = memberLine.Trim();
                                if (memberTrimmed.StartsWith("$"))
                                {
                                    // Target elision syntax - refers to resource property
                                    var memberName = memberTrimmed.Substring(1);
                                    var member = new MemberShape 
                                    { 
                                        Name = memberName, 
                                        Target = PascalCase(memberName) // Assume same name as type
                                    };
                                    outputStructure.Members.Add(member);
                                }
                            }
                            
                            model.Shapes.Add(outputStructure);
                            op.Output = outputStructureId;
                        }
                        else
                        {
                            // Legacy output parsing
                            var outputIdx = fullOperationText.IndexOf("output:");
                            if (outputIdx >= 0)
                            {
                                var afterOutput = fullOperationText.Substring(outputIdx + 7).TrimStart();
                                var outputVal = afterOutput.Split(' ', '}')[0].Trim();
                                op.Output = outputVal;
                            }
                        }
                    }
                    
                    // Parse errors
                    var errorsMatch = System.Text.RegularExpressions.Regex.Match(fullOperationText, @"errors:\s*\[([^\]]*)\]");
                    if (errorsMatch.Success)
                    {
                        var errorsList = errorsMatch.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        op.Errors.AddRange(errorsList);
                    }
                    
                    model.Shapes.Add(op);
                }
                else if (trimmed.StartsWith("structure "))
                {
                    var id = trimmed.Split(' ')[1];
                    var structure = new StructureShape { Id = id };
                    if (pendingDocumentation != null)
                    {
                        structure.Documentation = pendingDocumentation;
                        pendingDocumentation = null;
                    }
                    structure.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Handle multi-line structure definitions
                    var structureContent = new System.Text.StringBuilder();
                    structureContent.Append(trimmed);
                    
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            structureContent.AppendLine().Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullStructureText = structureContent.ToString();
                    var membersStart = fullStructureText.IndexOf('{');
                    var membersEnd = fullStructureText.LastIndexOf('}');
                    
                    if (membersStart >= 0 && membersEnd > membersStart)
                    {
                        var membersContent = fullStructureText.Substring(membersStart + 1, membersEnd - membersStart - 1);
                        ParseStructureMembers(structure, membersContent);
                    }
                    
                    model.Shapes.Add(structure);
                }
                // Simple type declarations (e.g., "string LocationId", "@pattern(...) string LocationId")
                else if (IsSimpleTypeDeclaration(trimmed))
                {
                    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var baseType = parts[0];
                        var typeName = parts[1];
                        
                        // Create a structure shape with the base type as target
                        var simpleTypeShape = new StructureShape { Id = typeName };
                        simpleTypeShape.ConstraintTraits.AddRange(pendingTraits);
                        
                        // Add a constraint trait to indicate this is an alias
                        simpleTypeShape.ConstraintTraits.Add(new ConstraintTrait 
                        { 
                            Name = "typeAlias", 
                            Properties = new Dictionary<string, object> { ["target"] = baseType }
                        });
                        
                        pendingTraits.Clear();
                        model.Shapes.Add(simpleTypeShape);
                    }
                }                else if (trimmed.StartsWith("list "))
                {
                    var id = trimmed.Split(' ')[1];
                    var listShape = new ListShape { Id = id };
                    if (pendingDocumentation != null)
                    {
                        listShape.Documentation = pendingDocumentation;
                        pendingDocumentation = null;
                    }
                    listShape.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Handle multi-line list definitions
                    var listContent = new System.Text.StringBuilder();
                    listContent.Append(trimmed);
                    
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            listContent.AppendLine().Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullListText = listContent.ToString();
                    
                    // Parse Smithy 2.0 syntax: member: TargetType
                    var memberMatch = System.Text.RegularExpressions.Regex.Match(fullListText, @"member:\s*(\w+)");
                    if (memberMatch.Success)
                    {
                        listShape.Member.Target = memberMatch.Groups[1].Value;
                    }
                    else
                    {
                        // Legacy syntax: memberType: TargetType
                        var memberTypeIdx = fullListText.IndexOf("memberType:");
                        if (memberTypeIdx >= 0)
                        {
                            var afterMemberType = fullListText.Substring(memberTypeIdx + 11).Trim();
                            listShape.Member.Target = afterMemberType.Split(' ', '}')[0].Trim();
                        }
                    }
                    
                    model.Shapes.Add(listShape);
                }
                else if (trimmed.StartsWith("set "))
                {
                    var id = trimmed.Split(' ')[1];
                    var setShape = new SetShape { Id = id };
                    if (pendingDocumentation != null)
                    {
                        setShape.Documentation = pendingDocumentation;
                        pendingDocumentation = null;
                    }
                    setShape.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Handle multi-line set definitions
                    var setContent = new System.Text.StringBuilder();
                    setContent.Append(trimmed);
                    
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            setContent.AppendLine().Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullSetText = setContent.ToString();
                    
                    // Parse Smithy 2.0 syntax: member: TargetType
                    var memberMatch = System.Text.RegularExpressions.Regex.Match(fullSetText, @"member:\s*(\w+)");
                    if (memberMatch.Success)
                    {
                        setShape.Member.Target = memberMatch.Groups[1].Value;
                    }
                    else
                    {
                        // Legacy syntax: memberType: TargetType
                        var memberTypeIdx = fullSetText.IndexOf("memberType:");
                        if (memberTypeIdx >= 0)
                        {
                            var afterMemberType = fullSetText.Substring(memberTypeIdx + 11).Trim();
                            setShape.Member.Target = afterMemberType.Split(' ', '}')[0].Trim();
                        }
                    }
                    
                    model.Shapes.Add(setShape);
                }
                else if (trimmed.StartsWith("map "))
                {
                    var id = trimmed.Split(' ')[1];
                    var mapShape = new MapShape { Id = id };
                    if (pendingDocumentation != null)
                    {
                        mapShape.Documentation = pendingDocumentation;
                        pendingDocumentation = null;
                    }
                    mapShape.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Handle multi-line map definitions
                    var mapContent = new System.Text.StringBuilder();
                    mapContent.Append(trimmed);
                    
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            mapContent.AppendLine().Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullMapText = mapContent.ToString();
                    
                    // Parse Smithy 2.0 syntax: key: KeyType, value: ValueType
                    var keyMatch = System.Text.RegularExpressions.Regex.Match(fullMapText, @"key:\s*(\w+)");
                    var valueMatch = System.Text.RegularExpressions.Regex.Match(fullMapText, @"value:\s*(\w+)");
                    
                    if (keyMatch.Success)
                    {
                        mapShape.Key.Target = keyMatch.Groups[1].Value;
                    }
                    else
                    {
                        // Legacy syntax: keyType: KeyType
                        var keyTypeIdx = fullMapText.IndexOf("keyType:");
                        if (keyTypeIdx >= 0)
                        {
                            var afterKeyType = fullMapText.Substring(keyTypeIdx + 8).Trim();
                            mapShape.Key.Target = afterKeyType.Split(' ', '}')[0].Trim();
                        }
                    }
                    
                    if (valueMatch.Success)
                    {
                        mapShape.Value.Target = valueMatch.Groups[1].Value;
                    }
                    else
                    {
                        // Legacy syntax: valueType: ValueType
                        var valueTypeIdx = fullMapText.IndexOf("valueType:");
                        if (valueTypeIdx >= 0)
                        {
                            var afterValueType = fullMapText.Substring(valueTypeIdx + 10).Trim();
                            mapShape.Value.Target = afterValueType.Split(' ', '}')[0].Trim();
                        }
                    }
                    
                    model.Shapes.Add(mapShape);
                }else if (trimmed.StartsWith("resource "))
                {
                    var id = trimmed.Split(' ')[1];
                    var resource = new ResourceShape { Id = id };
                    if (pendingDocumentation != null)
                    {
                        resource.Documentation = pendingDocumentation;
                        pendingDocumentation = null;
                    }
                    resource.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Handle multi-line resource definitions
                    var resourceContent = new System.Text.StringBuilder();
                    resourceContent.Append(trimmed);
                    
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            resourceContent.AppendLine().Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullResourceText = resourceContent.ToString();
                    ParseResourceProperties(resource, fullResourceText);
                    model.Shapes.Add(resource);
                }
                else if (trimmed.StartsWith("union "))
                {
                    var id = trimmed.Split(' ')[1];
                    var union = new UnionShape { Id = id };
                    union.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Union member 파싱 (구조체와 유사)
                    var membersStart = trimmed.IndexOf('{');
                    var membersEnd = trimmed.IndexOf('}');
                    if (membersStart >= 0 && membersEnd > membersStart)
                    {
                        var membersStr = trimmed.Substring(membersStart + 1, membersEnd - membersStart - 1).Trim();
                        if (!string.IsNullOrEmpty(membersStr))
                        {
                            var memberDefs = membersStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var memberDef in memberDefs)
                            {
                                var parts = memberDef.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                if (parts.Length == 2)
                                {
                                    var member = new MemberShape { Name = parts[0], Target = parts[1] };
                                    union.Members.Add(member);
                                }
                            }
                        }
                    }
                    model.Shapes.Add(union);
                }                else if (trimmed.StartsWith("enum "))
                {
                    var id = trimmed.Split(' ')[1];
                    var enumShape = new EnumShape { Id = id };
                    enumShape.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Enhanced multi-line enum parsing
                    var fullEnumContent = new System.Text.StringBuilder();
                    fullEnumContent.Append(trimmed);
                    
                    // Handle multi-line enum definitions
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            fullEnumContent.AppendLine().Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullEnumText = fullEnumContent.ToString();
                    var membersStart = fullEnumText.IndexOf('{');
                    var membersEnd = fullEnumText.LastIndexOf('}');
                    
                    if (membersStart >= 0 && membersEnd > membersStart)
                    {
                        var membersContent = fullEnumText.Substring(membersStart + 1, membersEnd - membersStart - 1);
                        var memberLines = membersContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var memberLine in memberLines)
                        {
                            var memberTrimmed = memberLine.Trim();
                            if (string.IsNullOrEmpty(memberTrimmed)) continue;
                            
                            // Parse member traits
                            List<ConstraintTrait> memberTraits = new();
                            while (memberTrimmed.StartsWith("@"))
                            {
                                int endIdx = memberTrimmed.IndexOf(' ');
                                if (endIdx == -1) endIdx = memberTrimmed.Length;
                                var traitPart = memberTrimmed.Substring(0, endIdx);
                                var trait = ParseConstraintTrait(traitPart);
                                if (trait != null) memberTraits.Add(trait);
                                memberTrimmed = memberTrimmed.Substring(endIdx).TrimStart();
                            }
                            
                            // Parse member name and value
                            var memberName = memberTrimmed;
                            var memberValue = memberTrimmed;
                            
                            if (memberTrimmed.Contains('='))
                            {
                                var parts = memberTrimmed.Split('=', 2, StringSplitOptions.TrimEntries);
                                if (parts.Length == 2)
                                {
                                    memberName = parts[0].Trim();
                                    memberValue = parts[1].Trim().Trim('"'); // Remove quotes if present
                                }
                            }
                            
                            var member = new EnumMemberShape { 
                                Name = memberName, 
                                Value = memberValue,
                                ConstraintTraits = memberTraits
                            };
                            enumShape.Members.Add(member);
                        }
                    }
                    model.Shapes.Add(enumShape);
                }                else if (trimmed.StartsWith("intEnum "))
                {
                    var id = trimmed.Split(' ')[1];
                    var intEnumShape = new IntEnumShape { Id = id };
                    intEnumShape.ConstraintTraits.AddRange(pendingTraits);
                    pendingTraits.Clear();
                    
                    // Enhanced multi-line intEnum parsing
                    var fullIntEnumContent = new System.Text.StringBuilder();
                    fullIntEnumContent.Append(trimmed);
                    
                    // Handle multi-line intEnum definitions
                    if (trimmed.Contains('{') && !trimmed.Contains('}'))
                    {
                        int braceCount = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                        while (braceCount > 0 && i + 1 < lines.Length)
                        {
                            i++;
                            var nextLine = lines[i].Trim();
                            fullIntEnumContent.AppendLine().Append(nextLine);
                            braceCount += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                        }
                    }
                    
                    var fullIntEnumText = fullIntEnumContent.ToString();
                    var membersStart = fullIntEnumText.IndexOf('{');
                    var membersEnd = fullIntEnumText.LastIndexOf('}');
                    
                    if (membersStart >= 0 && membersEnd > membersStart)
                    {
                        var membersContent = fullIntEnumText.Substring(membersStart + 1, membersEnd - membersStart - 1);
                        var memberLines = membersContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var memberLine in memberLines)
                        {
                            var memberTrimmed = memberLine.Trim();
                            if (string.IsNullOrEmpty(memberTrimmed)) continue;
                            
                            // Parse member traits
                            List<ConstraintTrait> memberTraits = new();
                            while (memberTrimmed.StartsWith("@"))
                            {
                                int endIdx = memberTrimmed.IndexOf(' ');
                                if (endIdx == -1) endIdx = memberTrimmed.Length;
                                var traitPart = memberTrimmed.Substring(0, endIdx);
                                var trait = ParseConstraintTrait(traitPart);
                                if (trait != null) memberTraits.Add(trait);
                                memberTrimmed = memberTrimmed.Substring(endIdx).TrimStart();
                            }
                            
                            // Parse member name = value
                            if (memberTrimmed.Contains('='))
                            {
                                var parts = memberTrimmed.Split('=', 2, StringSplitOptions.TrimEntries);
                                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var value))
                                {
                                    var member = new IntEnumMemberShape { 
                                        Name = parts[0].Trim(), 
                                        Value = value,
                                        ConstraintTraits = memberTraits
                                    };
                                    intEnumShape.Members.Add(member);
                                }
                            }
                        }
                    }
                    model.Shapes.Add(intEnumShape);
                }
            }
            return model;
        }

        // ConstraintTrait 파싱 헬퍼
        private ConstraintTrait? ParseConstraintTrait(string traitText)
        {
            // 예: @length(min: 1, max: 10)
            if (!traitText.StartsWith("@")) return null;
            int parenIdx = traitText.IndexOf('(');
            if (parenIdx == -1)
            {
                // 속성 없는 trait
                return new ConstraintTrait { Name = traitText.Substring(1) };
            }
            var name = traitText.Substring(1, parenIdx - 1);
            var propsText = traitText.Substring(parenIdx + 1, traitText.Length - parenIdx - 2); // 괄호 안
            var props = new Dictionary<string, object>();
            foreach (var pair in propsText.Split(','))
            {
                var kv = pair.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (kv.Length == 2)
                {
                    var key = kv[0];
                    var val = kv[1];
                    if (int.TryParse(val, out var intVal))
                        props[key] = intVal;
                    else if (double.TryParse(val, out var dblVal))
                        props[key] = dblVal;
                    else if (val.StartsWith("\"") && val.EndsWith("\""))
                        props[key] = val.Trim('"');
                    else
                        props[key] = val;
                }
            }
            return new ConstraintTrait { Name = name, Properties = props };
        }

        // 문자열을 적절한 C# 객체로 변환
        private object ParseValue(string valueText)
        {
            valueText = valueText.Trim();
            if (valueText.StartsWith("\"") && valueText.EndsWith("\""))
            {
                // 문자열
                return valueText[1..^1];
            }
            else if (valueText == "true" || valueText == "false")
            {
                // 불리언
                return bool.Parse(valueText);
            }
            else if (double.TryParse(valueText, out var doubleVal))
            {
                // 실수
                return doubleVal;
            }
            else
            {
                // 정수 또는 기타
                return valueText;
            }
        }

        // Helper method to check if a line is a simple type declaration
        private bool IsSimpleTypeDeclaration(string trimmed)
        {
            var simpleTypes = new[] { "string", "integer", "long", "short", "byte", "float", "double", 
                                    "boolean", "blob", "timestamp", "document", "bigInteger", "bigDecimal" };
            
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var firstPart = parts[0];
                return simpleTypes.Contains(firstPart);
            }
            return false;
        }
        
        // Helper method to parse inline structure definitions (input := { ... }, output := { ... })
        private StructureShape ParseInlineStructure(string structureId, string membersContent)
        {
            var structure = new StructureShape { Id = structureId };
            ParseStructureMembers(structure, membersContent);
            return structure;
        }
        
        // Enhanced structure member parsing for multi-line definitions
        private void ParseStructureMembers(StructureShape structure, string membersContent)
        {
            if (string.IsNullOrWhiteSpace(membersContent)) return;
            
            var lines = membersContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var currentMember = new System.Text.StringBuilder();
            var memberTraits = new List<ConstraintTrait>();
            string? memberDocumentation = null;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                
                // Handle documentation comments
                if (trimmed.StartsWith("///"))
                {
                    memberDocumentation = trimmed.Substring(3).Trim();
                    continue;
                }
                
                // Handle traits
                if (trimmed.StartsWith("@"))
                {
                    var trait = ParseConstraintTrait(trimmed);
                    if (trait != null) memberTraits.Add(trait);
                    continue;
                }
                
                // Handle member definition
                if (trimmed.Contains(':'))
                {
                    // If we have a previous member, process it
                    if (currentMember.Length > 0)
                    {
                        ProcessStructureMember(structure, currentMember.ToString(), memberTraits, memberDocumentation);
                        currentMember.Clear();
                        memberTraits.Clear();
                        memberDocumentation = null;
                    }
                    
                    currentMember.Append(trimmed);
                }
                else if (currentMember.Length > 0)
                {
                    // Continue the current member definition
                    currentMember.Append(' ').Append(trimmed);
                }
            }
            
            // Process the last member
            if (currentMember.Length > 0)
            {
                ProcessStructureMember(structure, currentMember.ToString(), memberTraits, memberDocumentation);
            }
        }
        
        // Process individual structure member
        private void ProcessStructureMember(StructureShape structure, string memberDefinition, List<ConstraintTrait> traits, string? documentation)
        {
            var parts = memberDefinition.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var memberName = parts[0].Trim();
                var memberTarget = parts[1].Trim();
                
                // Handle default values (e.g., units: TemperatureUnit = "celsius")
                if (memberTarget.Contains('='))
                {
                    var targetParts = memberTarget.Split('=', 2, StringSplitOptions.TrimEntries);
                    memberTarget = targetParts[0].Trim();
                    var defaultValue = targetParts[1].Trim().Trim('"');
                    
                    // Add default value as a trait
                    traits.Add(new ConstraintTrait 
                    { 
                        Name = "default", 
                        Properties = new Dictionary<string, object> { ["value"] = defaultValue }
                    });
                }
                
                var member = new MemberShape 
                { 
                    Name = memberName, 
                    Target = memberTarget,
                    Documentation = documentation,
                    ConstraintTraits = new List<ConstraintTrait>(traits)
                };
                
                structure.Members.Add(member);
            }
        }
        
        // Enhanced resource property parsing for modern Smithy 2.0 syntax
        private void ParseResourceProperties(ResourceShape resource, string resourceContent)
        {
            // Parse identifiers: { key: value, ... }
            var identifiersMatch = System.Text.RegularExpressions.Regex.Match(resourceContent, @"identifiers:\s*\{([^}]*)\}");
            if (identifiersMatch.Success)
            {
                var identifiersContent = identifiersMatch.Groups[1].Value;
                var pairs = identifiersContent.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (keyValue.Length == 2)
                    {
                        resource.Identifiers[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }
            
            // Parse properties: { property: Type, ... } (Smithy 2.0 feature)
            var propertiesMatch = System.Text.RegularExpressions.Regex.Match(resourceContent, @"properties:\s*\{([^}]*)\}");
            if (propertiesMatch.Success)
            {
                var propertiesContent = propertiesMatch.Groups[1].Value;
                var propertyLines = propertiesContent.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in propertyLines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    
                    var keyValue = trimmed.Split(':', 2, StringSplitOptions.TrimEntries);
                    if (keyValue.Length == 2)
                    {
                        resource.Properties[keyValue[0]] = keyValue[1];
                    }
                }
            }
            
            // Parse single-value fields
            foreach (var field in new[] { "create", "read", "update", "delete", "list", "put" })
            {
                var pattern = $@"{field}:\s*(\w+)";
                var match = System.Text.RegularExpressions.Regex.Match(resourceContent, pattern);
                if (match.Success)
                {
                    var propName = char.ToUpper(field[0]) + field.Substring(1);
                    typeof(ResourceShape).GetProperty(propName)?.SetValue(resource, match.Groups[1].Value);
                }
            }
            
            // Parse array fields
            foreach (var field in new[] { "operations", "collectionOperations", "resources" })
            {
                var pattern = $@"{field}:\s*\[([^\]]*)\]";
                var match = System.Text.RegularExpressions.Regex.Match(resourceContent, pattern);
                if (match.Success)
                {
                    var values = match.Groups[1].Value
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(v => v.Trim())
                        .ToArray();
                    
                    var propName = char.ToUpper(field[0]) + field.Substring(1);
                    var prop = typeof(ResourceShape).GetProperty(propName);
                    if (prop?.GetValue(resource) is List<string> list)
                    {
                        list.AddRange(values);
                    }
                }
            }
        }
        
        // Helper method for Pascal case conversion
        private string PascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }

    public class ListShape : Shape
    {
        public MemberShape Member { get; set; } = new();
    }
    public class SetShape : Shape
    {
        public MemberShape Member { get; set; } = new();
    }
    public class MapShape : Shape
    {
        public MemberShape Key { get; set; } = new();
        public MemberShape Value { get; set; } = new();
    }    public class ResourceShape : Shape
    {
        public Dictionary<string, string> Identifiers { get; set; } = new();
        public Dictionary<string, string> Properties { get; set; } = new(); // Smithy 2.0 properties
        public string? Create { get; set; }
        public string? Put { get; set; }
        public string? Read { get; set; }
        public string? Update { get; set; }
        public string? Delete { get; set; }
        public string? List { get; set; }
        public List<string> Operations { get; set; } = new();
        public List<string> CollectionOperations { get; set; } = new();
        public List<string> Resources { get; set; } = new();
    }
    public class UnionShape : Shape
    {
        public List<MemberShape> Members { get; set; } = new();
    }

    public class EnumShape : Shape
    {
        public List<EnumMemberShape> Members { get; set; } = new();
    }

    public class IntEnumShape : Shape
    {
        public List<IntEnumMemberShape> Members { get; set; } = new();
    }

    public class EnumMemberShape
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Documentation { get; set; }
        public List<ConstraintTrait> ConstraintTraits { get; set; } = new();
    }

    public class IntEnumMemberShape
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string? Documentation { get; set; }
        public List<ConstraintTrait> ConstraintTraits { get; set; } = new();
    }

    public interface ISmithyModelValidator
    {
        List<string> Validate(SmithyModel model);
    }

    public class SmithyModelValidator : ISmithyModelValidator
    {
        public List<string> Validate(SmithyModel model)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var shape in model.Shapes)
            {
                if (string.IsNullOrWhiteSpace(shape.Id))
                    errors.Add($"Shape ID is required: {shape.GetType().Name}");
                else if (!ids.Add(shape.Id))
                    errors.Add($"Duplicate shape ID: {shape.Id}");
            }
            return errors;
        }
    }
}
