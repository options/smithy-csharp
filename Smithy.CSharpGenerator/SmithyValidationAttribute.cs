using System;

namespace Smithy.CSharpGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class SmithyValidationAttribute : Attribute
    {
        public string Message { get; }
        public SmithyValidationAttribute(string message)
        {
            Message = message;
        }
    }
}
