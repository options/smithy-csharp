using System;

namespace Smithy.CSharpGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class EndpointHostPrefixAttribute : Attribute
    {
        public string HostPrefix { get; }
        public EndpointHostPrefixAttribute(string hostPrefix)
        {
            HostPrefix = hostPrefix;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class EndpointUrlAttribute : Attribute
    {
        public string Url { get; }
        public EndpointUrlAttribute(string url)
        {
            Url = url;
        }
    }
}
