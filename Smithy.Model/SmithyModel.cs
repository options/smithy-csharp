using System.Collections.Generic;

using System.Linq;

namespace Smithy.Model
{
    public class SmithyModel
    {
        public List<Shape> Shapes { get; set; } = new();
        // Smithy IDL extensions
        public string? Namespace { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> Uses { get; set; } = new();
        public List<string> Applies { get; set; } = new();
          /// <summary>
        /// Get a shape by its ID from the model
        /// </summary>
        /// <param name="shapeId">The ID of the shape to retrieve</param>
        /// <returns>The shape with the specified ID, or null if not found</returns>
        public Shape? GetShape(string? shapeId)
        {
            if (string.IsNullOrEmpty(shapeId))
                return null;
            
            return Shapes.FirstOrDefault(s => s.Id == shapeId);
        }
        
        /// <summary>
        /// Get a shape by its ID from the model
        /// </summary>
        /// <typeparam name="T">The type of shape to retrieve</typeparam>
        /// <param name="shapeId">The ID of the shape to retrieve</param>
        /// <returns>The shape with the specified ID, or null if not found</returns>
        public T? GetShape<T>(string? shapeId) where T : Shape
        {
            if (string.IsNullOrEmpty(shapeId))
                return null;
            
            return Shapes.OfType<T>().FirstOrDefault(s => s.Id == shapeId);
        }
    }

    public interface ISmithyModelParser
    {
        SmithyModel Parse(string smithySource);
    }
}
