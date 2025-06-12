using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Smithy.Model
{
    public class SmithyJsonAstParser : ISmithyModelParser
    {
        public SmithyModel Parse(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var doc = JsonNode.Parse(json);
            if (doc == null) throw new Exception("Invalid JSON AST");

            var model = new SmithyModel();
            model.Namespace = doc["smithy"]?.ToString();

            // Metadata
            if (doc["metadata"] is JsonObject metadataObj)
            {
                model.Metadata = new Dictionary<string, object>();
                foreach (var kv in metadataObj)
                    model.Metadata[kv.Key] = kv.Value?.ToString();
            }

            // Shapes
            if (doc["shapes"] is JsonObject shapesObj)
            {
                foreach (var shapeKvp in shapesObj)
                {
                    var shapeId = shapeKvp.Key;
                    var shapeNode = shapeKvp.Value as JsonObject;
                    if (shapeNode == null) continue;
                    var type = shapeNode["type"]?.ToString();
                    switch (type)
                    {
                        case "structure":
                            var structure = new StructureShape { Id = shapeId };
                            if (shapeNode["members"] is JsonObject membersObj)
                            {
                                foreach (var memKvp in membersObj)
                                {
                                    var memName = memKvp.Key;
                                    var memTarget = memKvp.Value?["target"]?.ToString() ?? memKvp.Value?.ToString();
                                    structure.Members.Add(new MemberShape { Name = memName, Target = memTarget });
                                }
                            }
                            model.Shapes.Add(structure);
                            break;
                        case "string":
                            model.Shapes.Add(new StringShape(shapeId));
                            break;
                        case "list":
                            model.Shapes.Add(new ListShape { Id = shapeId });
                            break;
                        case "map":
                            model.Shapes.Add(new MapShape { Id = shapeId });
                            break;
                        // Add more shape types as needed
                        default:
                            // Skip unknown/abstract types for now
                            break;
                    }
                }
            }
            // TODO: Parse traits, applies, uses, etc.
            return model;
        }

        // 내부용: string shape
        private class StringShape : Shape { public StringShape(string id) { Id = id; } }
    }
}
