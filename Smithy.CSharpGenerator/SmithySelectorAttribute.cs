using System;

namespace Smithy.CSharpGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class SmithySelectorAttribute : Attribute
    {
        public string Expression { get; }
        public SmithySelectorAttribute(string expression)
        {
            Expression = expression;
        }
    }
}
