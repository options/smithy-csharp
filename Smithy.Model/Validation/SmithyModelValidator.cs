using System.Collections.Generic;
using System.Linq;

namespace Smithy.Model.Validation
{
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
