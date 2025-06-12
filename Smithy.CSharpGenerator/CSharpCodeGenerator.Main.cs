using Smithy.Model;
using Smithy.CSharpGenerator.Formatters;
using Smithy.CSharpGenerator.Utils;
using Smithy.CSharpGenerator.TypeMapping;
using Smithy.CSharpGenerator.Generators;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Smithy.CSharpGenerator;

public partial class CSharpCodeGenerator
{
    private readonly ConstraintAttributeGenerator _constraintAttributeGenerator = new();
    private readonly HttpProtocolGenerator _httpProtocolGenerator = new();
    private readonly ServiceGenerator _serviceGenerator = new();
    private readonly StructureGenerator _structureGenerator = new();
    private readonly ResourceGenerator _resourceGenerator = new();

    private void GenerateUsingStatements(StringBuilder inner)
    {
        inner.AppendLine("using System;");
        inner.AppendLine("using System.Collections.Generic;");
        inner.AppendLine("using System.ComponentModel.DataAnnotations;");
        inner.AppendLine("using System.Runtime.Serialization;");
        inner.AppendLine("using System.Text.Json;");
        inner.AppendLine("using System.Threading.Tasks;");
        inner.AppendLine("using Microsoft.AspNetCore.Cors;");
        inner.AppendLine("using Microsoft.AspNetCore.Mvc;");
        inner.AppendLine();
    }
}
