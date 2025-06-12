using System;
using System.Linq;

namespace Smithy.Model
{
    /// <summary>
    /// Extension methods for SmithyModel to simplify shape access
    /// </summary>
    public static class SmithyModelExtensions
    {
        /// <summary>
        /// Get a shape by its ID from the model
        /// </summary>
        /// <typeparam name="T">The type of shape to retrieve</typeparam>
        /// <param name="model">The Smithy model</param>
        /// <param name="shapeId">The ID of the shape to retrieve</param>
        /// <returns>The shape with the specified ID, or null if not found</returns>
        public static T GetShape<T>(this SmithyModel model, string shapeId) where T : Shape
        {
            if (model == null || string.IsNullOrEmpty(shapeId))
                return null;
            
            return model.Shapes.OfType<T>().FirstOrDefault(s => s.Id == shapeId);
        }
        
        /// <summary>
        /// Get a shape by its ID from the model
        /// </summary>
        /// <param name="model">The Smithy model</param>
        /// <param name="shapeId">The ID of the shape to retrieve</param>
        /// <returns>The shape with the specified ID, or null if not found</returns>
        public static Shape GetShape(this SmithyModel model, string shapeId)
        {
            if (model == null || string.IsNullOrEmpty(shapeId))
                return null;
            
            return model.Shapes.FirstOrDefault(s => s.Id == shapeId);
        }
    }
}
